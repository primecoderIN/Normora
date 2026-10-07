using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Normora.Api.Hubs;
using Normora.Modules.Documents.Persistence;
using Normora.Shared;
using Normora.Shared.Constants;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Command to upload a new document or a new version of an existing document.
/// </summary>
public record UploadDocumentCommand(IFormFile File, Guid TenantId, IReadOnlyCollection<Guid>? DepartmentIds = null, Guid? DocumentId = null) : IRequest<DocumentDto>;

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
    IHubContext<DocumentHub> hubContext) : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // Persist the binary before metadata so a database row never points to an object
        // that was not successfully stored. The tenant ID becomes part of the object key.
        var objectName = await storageService.UploadDocumentAsync(request.File, request.TenantId.ToString());

        // SEC-4: All post-upload work is wrapped in a try/catch so that if any subsequent
        // step fails (document lookup, DB persistence), the already-uploaded MinIO object
        // is deleted — preventing accumulation of unreferenced orphan objects in storage.
        try
        {
            // 2. Create EF Core Record (or fetch existing)
            Document document;
            int nextVersionNumber = 1;

            if (request.DocumentId.HasValue)
            {
                // NEW VERSION UPLOAD (Phase 19):
                // If the request contains a DocumentId, we are appending a new version to an existing document.
                document = await context.Documents
                    .Include(d => d.Versions)
                    .SingleOrDefaultAsync(d => d.Id == request.DocumentId.Value && d.TenantId == request.TenantId, cancellationToken);

                if (document == null) throw new InvalidOperationException(ApiMessages.DocumentNotFound);

                if (document.Versions.Any())
                {
                    // Assign sequential version number based on the previous max version.
                    nextVersionNumber = document.Versions.Max(v => v.VersionNumber) + 1;

                    // (Phase 19): Deactivate all older versions of this document.
                    // The new version being uploaded will be marked as IsActive = true below.
                    // This guarantees only one active version is used by the retrieval/RAG pipeline.
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

            // NEW VERSION UPLOAD (Phase 19):
            // Create the new DocumentVersion entry.
            // It carries the minio object name and the Active flag.
            var version = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                TenantId = request.TenantId,
                VersionNumber = nextVersionNumber,
                MinioObjectName = objectName,
                Status = DocumentStatus.Uploaded,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            document.Versions.Add(version);
            context.DocumentVersions.Add(version);
            await context.SaveChangesAsync(cancellationToken);

            // Notify connected tenant members before queueing background work so clients observe
            // the lifecycle in order: Uploaded, then Processing/Ready/Failed.
            await hubContext.Clients.Group(DocumentHub.GroupName(document.TenantId))
                .SendAsync("DocumentStatusChanged", new DocumentStatusChanged(
                    document.Id,
                    document.TenantId,
                    document.FileName,
                    version.Status.ToString()), cancellationToken);

            backgroundJobClient.Enqueue<DocumentProcessingJob>(job =>
                job.ProcessAsync(version.Id, document.TenantId));

            return document.ToDto();
        }
        catch
        {
            // SEC-4: Best-effort cleanup of the orphaned MinIO object.
            // Swallow secondary exceptions so the original error is preserved and propagated.
            try { await storageService.DeleteDocumentAsync(objectName); } catch { /* intentionally swallowed */ }
            throw;
        }
    }
}
