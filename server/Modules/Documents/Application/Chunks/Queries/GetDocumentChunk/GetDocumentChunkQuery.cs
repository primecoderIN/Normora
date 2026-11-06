using MediatR;
using Normora.Modules.Documents.Application.Chunks.DTOs;

namespace Normora.Modules.Documents.Application.Chunks.Queries;

/// <summary>
/// Query to retrieve a specific text chunk for citation previews, enforcing tenant isolation and department RBAC.
/// </summary>
public record GetDocumentChunkQuery(Guid ChunkId) : IRequest<DocumentChunkPreviewDto>;

