using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Infrastructure;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Command to delete a document by its ID. Requires the TenantId for authorization and isolation.
/// </summary>
public record DeleteDocumentCommand(Guid Id, Guid TenantId) : IRequest<bool>;

public sealed class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
    }
}

/// <summary>
/// Handles the execution of DeleteDocumentCommand.
/// Responsible for removing the physical file from MinIO and deleting the metadata record from the database.
/// </summary>
public sealed class DeleteDocumentCommandHandler(AppDbContext context, IDocumentStorageService storageService) : IRequestHandler<DeleteDocumentCommand, bool>
{
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the document, explicitly ensuring it belongs to the current tenant.
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == request.TenantId, cancellationToken);

        if (document == null)
        {
            return false;
        }

        // 2. Remove from S3 / MinIO storage.
        await storageService.DeleteDocumentAsync(document.MinioObjectName);
            
        // 3. Remove metadata from the database.
        context.Documents.Remove(document);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
