using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Commands;

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
        // Broken Object Level Authorization (BOLA) & Soft-Delete Filter:
        // The DocumentsDbContext global query filter applies: TenantId == _tenantContext.TenantId && DeletedAt == null.
        // If the document does not exist, belongs to another tenant, or is already deleted, FirstOrDefaultAsync returns null.
        // We throw BolaException (mapped to 404) to avoid leaking resource existence.
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (document is null)
        {
            throw new BolaException();
        }

        // Soft-Deletion Invariant:
        // Instead of immediate physical deletion, we stamp DeletedAt and DeletedByUserId.
        // Benefits:
        // 1. Instantly hides the document and its chunks from all conversational RAG retrievals and queries.
        // 2. Preserves an audit trail for compliance and inadvertent deletion recovery during the grace period (30 days).
        // 3. Physical purging of S3 binaries and pgvector embeddings is decoupled into PurgeDeletedDocumentsJob.
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedByUserId = request.DeletedByUserId;
        await context.SaveChangesAsync(cancellationToken);

        // Distributed Cache Eviction:
        // Invalidate cached dashboard metrics so document count and storage graphs update immediately.
        await cache.RemoveAsync($"dashboard:{document.TenantId}", cancellationToken);

        // Real-Time SignalR Broadcast:
        // Broadcasts to all connected employer sessions in the tenant group to instantly remove
        // the document row from the UI data table without requiring a browser refresh.
        await notificationService.SendDocumentDeletedAsync(document.TenantId, document.Id, cancellationToken);

        logger.LogInformation(
            "Document {DocumentId} soft-deleted by user {UserId} in tenant {TenantId}.",
            document.Id,
            request.DeletedByUserId,
            document.TenantId);

        return true;
    }
}

