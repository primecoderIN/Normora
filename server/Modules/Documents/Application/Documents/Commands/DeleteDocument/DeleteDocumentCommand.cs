using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Commands;

/// <summary>
/// Command to soft-delete a document by marking it as deleted in the database.
/// Handled by <see cref="DeleteDocumentCommandHandler"/>.
/// </summary>
/// <remarks>
/// Lifecycle & Soft-Delete Retention:
/// - Verifies document ownership within ambient TenantId (BOLA protection).
/// - Sets DeletedAt and DeletedByUserId; excluded immediately from RAG searches via query filters.
/// - The physical MinIO objects and pgvector chunks are retained during a 30-day grace period
///   before the nightly <see cref="Jobs.PurgeDeletedDocumentsJob"/> permanently purges blobs and vectors.
/// - Implements <see cref="IAuditableRequest"/> to record deletion in the compliance audit log.
/// </remarks>
/// <param name="Id">The unique identifier of the document.</param>
/// <param name="TenantId">Target organization identifier.</param>
/// <param name="DeletedByUserId">The administrator initiating the soft deletion.</param>
public record DeleteDocumentCommand(Guid Id, Guid TenantId, Guid DeletedByUserId) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Document.Deleted";
    public string TargetType => "Document";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => null;
}

