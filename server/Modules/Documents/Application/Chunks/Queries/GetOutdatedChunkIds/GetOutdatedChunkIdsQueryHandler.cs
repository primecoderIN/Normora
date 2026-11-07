using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;

namespace Normora.Modules.Documents.Application.Chunks.Queries;

/// <summary>
/// Cross-module query handler that identifies document chunks belonging to deactivated or superseded document versions.
/// Used during conversational RAG retrieval to prevent the AI model from hallucinating or answering from outdated company policies.
/// </summary>
/// <remarks>
/// Version Invalidation:
/// - Joins candidate chunk IDs against DocumentVersions where `IsActive == false`.
/// - Returns HashSet of outdated Chunk IDs for O(1) exclusion during hybrid candidate pruning.
/// </remarks>
public class GetOutdatedChunkIdsQueryHandler(DocumentsDbContext context)
    : IRequestHandler<GetOutdatedChunkIdsQuery, HashSet<Guid>>
{
    public async Task<HashSet<Guid>> Handle(GetOutdatedChunkIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.ChunkIds.Count == 0)
        {
            return [];
        }

        // Identify chunks belonging to inactive document versions
        var outdatedIds = await context.DocumentChunks
            .AsNoTracking()
            .Where(c => request.ChunkIds.Contains(c.Id))
            .Join(
                context.DocumentVersions.AsNoTracking().Where(v => !v.IsActive),
                c => c.DocumentVersionId,
                v => v.Id,
                (c, v) => c.Id)
            .ToListAsync(cancellationToken);

        return outdatedIds.ToHashSet();
    }
}
