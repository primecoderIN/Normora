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
/// Command to upload a new document. Includes the physical file and the active TenantId.
/// </summary>
public record UploadDocumentCommand(IFormFile File, Guid TenantId) : IRequest<Document>;

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
            .Must(BeAValidExtension).WithMessage("Only .pdf, .docx, and .txt files are allowed.");
    }

    private bool BeAValidExtension(IFormFile? file)
    {
        if (file == null) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext == ".pdf" || ext == ".docx" || ext == ".txt";
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
    IHubContext<DocumentHub> hubContext) : IRequestHandler<UploadDocumentCommand, Document>
{
    public async Task<Document> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. Upload the physical file to MinIO object storage.
        var objectName = await storageService.UploadDocumentAsync(request.File, request.TenantId.ToString());

        // 2. Create EF Core Record
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.File.FileName,
            MinioObjectName = objectName,
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
            TenantId = request.TenantId
        };

        // 3. Save the document metadata to the PostgreSQL database.
        // NOTE: The DocumentsDbContext is configured with a Global Query Filter and an Interceptor
        // that will automatically bind this Document to the current active TenantId upon SaveChanges.
        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group(DocumentHub.GroupName(document.TenantId))
            .SendAsync("DocumentStatusChanged", new DocumentStatusChanged(
                document.Id,
                document.TenantId,
                document.FileName,
                document.Status.ToString()), cancellationToken);

        backgroundJobClient.Enqueue<DocumentProcessingJob>(job =>
            job.ProcessAsync(document.Id, document.TenantId));

        return document;
    }
}
