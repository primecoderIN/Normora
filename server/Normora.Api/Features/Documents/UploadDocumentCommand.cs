using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Normora.Api.Hubs;
using Normora.Modules.Documents.Persistence;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Command to upload a new document. Includes the physical file, the active TenantId,
/// and optionally a list of department IDs to scope the document to.
/// </summary>
public record UploadDocumentCommand(IFormFile File, Guid TenantId, IReadOnlyCollection<Guid>? DepartmentIds = null) : IRequest<DocumentDto>;

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

        // 2. Create EF Core Record
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.File.FileName,
            MinioObjectName = objectName,
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
            TenantId = request.TenantId,
            DocumentDepartments = request.DepartmentIds?.Select(depId => new DocumentDepartment
            {
                DepartmentId = depId
                // TenantId and DocumentId are populated automatically by EF Core conventions
                // and the DbContext SaveChanges interception.
            }).ToList() ?? new List<DocumentDepartment>()
        };

        // 3. Save the document metadata to the PostgreSQL database.
        // NOTE: The DocumentsDbContext is configured with a Global Query Filter and an Interceptor
        // that will automatically bind this Document to the current active TenantId upon SaveChanges.
        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        // Notify connected tenant members before queueing background work so clients observe
        // the lifecycle in order: Uploaded, then Processing/Ready/Failed.
        await hubContext.Clients.Group(DocumentHub.GroupName(document.TenantId))
            .SendAsync("DocumentStatusChanged", new DocumentStatusChanged(
                document.Id,
                document.TenantId,
                document.FileName,
                document.Status.ToString()), cancellationToken);

        backgroundJobClient.Enqueue<DocumentProcessingJob>(job =>
            job.ProcessAsync(document.Id, document.TenantId));

        return document.ToDto();
    }
}
