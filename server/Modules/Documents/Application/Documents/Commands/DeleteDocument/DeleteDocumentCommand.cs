using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Commands;

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

