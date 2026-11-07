using MediatR;
using Normora.Modules.Documents.Application.Chunks.DTOs;

namespace Normora.Modules.Documents.Application.Chunks.Queries;

/// <summary>
/// Query to retrieve a specific text chunk for citation previews, enforcing tenant isolation and department RBAC.
/// Handled by <see cref="GetDocumentChunkQueryHandler"/>.
/// </summary>
/// <remarks>
/// Security & Citation Authorization:
/// - Verifies that the requested chunk belongs to the caller's ambient TenantId (BOLA protection).
/// - Validates that the caller's assigned departments encompass the document's scoping (or document is Company-Wide).
/// - Returns text excerpt with source document metadata to power the slide-out citation preview pane.
/// </remarks>
/// <param name="ChunkId">The unique identifier of the <see cref="Domain.DocumentChunk"/> record.</param>
public record GetDocumentChunkQuery(Guid ChunkId) : IRequest<DocumentChunkPreviewDto>;

