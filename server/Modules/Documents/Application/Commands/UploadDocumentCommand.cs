using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Application.Dtos;
using Normora.Modules.Documents.Application.Jobs;
using Normora.Shared;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Commands;

/// <summary>
/// Command to upload a new document or a new version of an existing document.
/// </summary>
public record UploadDocumentCommand(
    IFormFile File,
    Guid TenantId,
    IReadOnlyCollection<Guid>? DepartmentIds = null,
    Guid? DocumentId = null) : IRequest<DocumentDto>, IAuditableRequest
{
    public string Action => DocumentId.HasValue ? "Document.VersionCreated" : "Document.Uploaded";
    public string TargetType => "Document";
    public string? GetTargetId(object? response) => response is DocumentDto doc ? doc.Id.ToString() : DocumentId?.ToString();
    public string? GetMetadata(object? response) => response is DocumentDto doc
        ? System.Text.Json.JsonSerializer.Serialize(new { fileName = doc.FileName, status = doc.Status, versionCount = doc.Versions.Count })
        : System.Text.Json.JsonSerializer.Serialize(new { fileName = File?.FileName, fileSize = File?.Length });
}

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("No file was uploaded.")
            .Must(file => file?.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file?.Length <= 20_971_520).WithMessage("File exceeds 20MB limit.")
            .Must(BeAValidExtension).WithMessage("Only .pdf, .docx, and .txt files are allowed.")
            .Must(HaveValidMagicBytes).WithMessage("File content does not match the declared file type.");
    }

    private static bool BeAValidExtension(IFormFile? file)
    {
        if (file == null) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext == ".pdf" || ext == ".docx" || ext == ".txt";
    }

    /// <summary>
    /// Validates the actual file content against known magic byte signatures.
    /// Prevents attackers from renaming a malicious file (e.g. .exe) to a trusted extension.
    /// </summary>
    private static bool HaveValidMagicBytes(IFormFile? file)
    {
        if (file == null || file.Length < 4) return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        // TXT has no standard magic bytes — trust the extension
        if (ext == ".txt") return true;

        Span<byte> header = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read < 4) return false;

        return ext switch
        {
            // PDF: starts with %PDF (0x25 0x50 0x44 0x46)
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            // DOCX is a ZIP archive: starts with PK (0x50 0x4B 0x03 0x04)
            ".docx" => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _ => false
        };
    }
}

/// <summary>
/// Handles the execution of UploadDocumentCommand.
/// Responsible for streaming the file to MinIO storage and saving metadata to the database.
/// </summary>
public sealed class UploadDocumentCommandHandler(
    DocumentsDbContext context,
    IDocumentStorageService storageService,
    IBackgroundJobClient backgroundJobClient,
    IDocumentNotificationService notificationService,
    IMediator mediator,
    ICacheService cache) : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // Enforce subscription plan limits before initiating binary storage upload
        await mediator.Send(new Normora.Shared.Contracts.Tenants.CheckTenantUploadLimitQuery(
            request.TenantId,
            request.File.Length,
            !request.DocumentId.HasValue), cancellationToken);

        // Persist the binary in object storage before database metadata is created.
        var objectName = await storageService.UploadDocumentAsync(request.File, request.TenantId.ToString());

        // Wrap subsequent operations in a compensating try/catch to delete the MinIO object if database persistence fails
        try
        {
            Document document;
            int nextVersionNumber = 1;

            if (request.DocumentId.HasValue)
            {
                // Append a new version to an existing document container
                var existingDoc = await context.Documents
                    .Include(d => d.Versions)
                    .SingleOrDefaultAsync(d => d.Id == request.DocumentId.Value && d.TenantId == request.TenantId, cancellationToken);

                if (existingDoc == null) throw new BolaException();
                document = existingDoc;

                if (document.Versions.Any())
                {
                    // Compute next sequential version number
                    nextVersionNumber = document.Versions.Max(v => v.VersionNumber) + 1;

                    // Deactivate prior versions so the retrieval pipeline only matches against the newest revision
                    foreach (var v in document.Versions)
                    {
                        v.IsActive = false;
                    }
                }
            }
            else
            {
                document = new Document
                {
                    Id = Guid.NewGuid(),
                    FileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    Size = request.File.Length,
                    UploadedAt = DateTime.UtcNow,
                    TenantId = request.TenantId,
                    DocumentDepartments = request.DepartmentIds?.Select(depId => new DocumentDepartment
                    {
                        DepartmentId = depId
                    }).ToList() ?? new List<DocumentDepartment>()
                };
                context.Documents.Add(document);
            }

            // Create the new active DocumentVersion record
            var version = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                TenantId = request.TenantId,
                VersionNumber = nextVersionNumber,
                StorageObjectName = objectName,
                Status = DocumentStatus.Uploaded,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            document.Versions.Add(version);
            context.DocumentVersions.Add(version);
            await context.SaveChangesAsync(cancellationToken);

            // Invalidate cached dashboard metrics for this tenant
            await cache.RemoveAsync($"dashboard:{document.TenantId}", cancellationToken);

            // Broadcast status change via notification service before scheduling background work
            await notificationService.SendDocumentStatusChangedAsync(
                document.TenantId,
                document.Id,
                document.FileName,
                version.Status.ToString(),
                cancellationToken);

            backgroundJobClient.Enqueue<DocumentProcessingJob>(job =>
                job.ProcessAsync(version.Id, document.TenantId));

            return document.ToDto();
        }
        catch
        {
            // Best-effort compensation: purge the uploaded binary from MinIO storage on failure
            try { await storageService.DeleteDocumentAsync(objectName); } catch { /* intentionally swallowed */ }
            throw;
        }
    }
}
