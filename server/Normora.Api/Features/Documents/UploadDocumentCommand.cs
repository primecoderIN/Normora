using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Normora.Infrastructure;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

public record UploadDocumentCommand(IFormFile File, string EmployerId) : IRequest<Document>;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.EmployerId)
            .NotEmpty().WithMessage("EmployerId is required.");

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

public sealed class UploadDocumentCommandHandler(AppDbContext context, IDocumentStorageService storageService) : IRequestHandler<UploadDocumentCommand, Document>
{
    public async Task<Document> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. Upload to MinIO
        var objectName = await storageService.UploadDocumentAsync(request.File, request.EmployerId);

        // 2. Create EF Core Record
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.File.FileName,
            MinioObjectName = objectName,
            Status = DocumentStatus.Processing,
            UploadedAt = DateTime.UtcNow,
            EmployerId = request.EmployerId
        };

        // 3. Save to DB
        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        return document;
    }
}
