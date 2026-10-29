using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents;

/// <summary>
/// Command to soft-delete a document by marking it as deleted in the database.
/// The physical MinIO objects and chunks are retained until the nightly
/// purge job purges records older than the grace period.
/// </summary>
public record DeleteDocumentCommand(Guid Id, Guid TenantId, Guid DeletedByUserId) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Document.Deleted";
    public string TargetType => "Document";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => null;
}

public sealed class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.DeletedByUserId).NotEmpty().WithMessage("DeletedByUserId is required.");
    }
}

/// <summary>
/// Handles the execution of <see cref="DeleteDocumentCommand"/>.
/// Marks the document as soft-deleted. Physical cleanup (MinIO + chunks) is deferred
/// to <see cref="Normora.Modules.Documents.Application.Jobs.PurgeDeletedDocumentsJob"/> which runs nightly after the grace period.
/// </summary>
public sealed class DeleteDocumentCommandHandler(
    DocumentsDbContext context,
    IDocumentNotificationService notificationService,
    ICacheService cache,
    ILogger<DeleteDocumentCommandHandler> logger) : IRequestHandler<DeleteDocumentCommand, bool>
{
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        // The global query filter already applies tenant + DeletedAt IS NULL isolation,
        // so a document from another tenant or already-deleted document will not be found.
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (document is null)
        {
            throw new BolaException();
        }

        // Soft-delete: set the audit fields. The global query filter will now exclude
        // this document from all future queries including RAG retrieval automatically.
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedByUserId = request.DeletedByUserId;
        await context.SaveChangesAsync(cancellationToken);

        // Invalidate cached dashboard metrics for this tenant
        await cache.RemoveAsync($"dashboard:{document.TenantId}", cancellationToken);

        // Broadcast to all employer sessions connected to this tenant so the document
        // row is removed from the list in real time without a page refresh.
        await notificationService.SendDocumentDeletedAsync(document.TenantId, document.Id, cancellationToken);

        logger.LogInformation(
            "Document {DocumentId} soft-deleted by user {UserId} in tenant {TenantId}.",
            document.Id,
            request.DeletedByUserId,
            document.TenantId);

        return true;
    }
}
