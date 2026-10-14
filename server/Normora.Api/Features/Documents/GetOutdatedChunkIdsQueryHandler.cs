using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;

namespace Normora.Api.Features.Documents;

public class GetOutdatedChunkIdsQueryHandler(DocumentsDbContext context)
    : IRequestHandler<GetOutdatedChunkIdsQuery, HashSet<Guid>>
{
    public async Task<HashSet<Guid>> Handle(GetOutdatedChunkIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.ChunkIds.Count == 0)
        {
            return [];
        }

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
