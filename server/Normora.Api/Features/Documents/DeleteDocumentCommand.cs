using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Normora.Api.Hubs;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Exceptions;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Command to soft-delete a document by marking it as deleted in the database.
/// The physical MinIO objects and chunks are retained until the nightly
/// <see cref="PurgeDeletedDocumentsJob"/> purges records older than the grace period.
/// </summary>
public record DeleteDocumentCommand(Guid Id, Guid TenantId, Guid DeletedByUserId) : IRequest<bool>;

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
/// to <see cref="PurgeDeletedDocumentsJob"/> which runs nightly after the grace period.
/// </summary>
public sealed class DeleteDocumentCommandHandler(
    DocumentsDbContext context,
    IHubContext<DocumentHub> hubContext,
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

        // Broadcast to all employer sessions connected to this tenant so the document
        // row is removed from the list in real time without a page refresh.
        await hubContext.Clients
            .Group(DocumentHub.GroupName(document.TenantId))
            .SendAsync("DocumentDeleted", document.Id, cancellationToken);

        logger.LogInformation(
            "Document {DocumentId} soft-deleted by user {UserId} in tenant {TenantId}.",
            document.Id,
            request.DeletedByUserId,
            document.TenantId);

        return true;
    }
}
