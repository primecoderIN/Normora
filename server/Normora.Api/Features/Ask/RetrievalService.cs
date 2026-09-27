using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Interfaces;
using Pgvector.EntityFrameworkCore;
using Pgvector;
using Normora.Api.Features.Documents;

namespace Normora.Api.Features.Ask;

public sealed class RetrievalService(
    DocumentsDbContext documentsContext,
    ITenantContext tenantContext,
    ITextEmbeddingService embeddingService) : IRetrievalService
{
    private const double RrfK = 60.0;

    public async Task<(List<RetrievalCandidate> Candidates, double VectorSimOfFirst)>
        RunHybridRetrievalAsync(string question, int limit, Guid? personalTenantId, CancellationToken ct = default)
    {
        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(question, ct));
        var effectiveDepartments = tenantContext.EffectiveDepartments;
        var activeTenantId = tenantContext.TenantId;

        var queryWords = question.Split([' ', '\t', '\n', '\r', '.', ',', '?', '!', '\''],
            StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length > 2).ToArray();
        var tsQueryText = queryWords.Length > 0 ? string.Join(" | ", queryWords) : null;

        var baseQuery = documentsContext.DocumentChunks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                documentsContext.Documents.IgnoreQueryFilters().AsNoTracking().Where(d =>
                    !d.DocumentDepartments.Any() ||
                    d.DocumentDepartments.Any(dd => effectiveDepartments.Contains(dd.DepartmentId))),
                chunk => chunk.DocumentId,
                doc => doc.Id,
                (chunk, doc) => new { chunk, doc })
            .Where(x => x.chunk.TenantId == activeTenantId || (personalTenantId.HasValue && x.chunk.TenantId == personalTenantId.Value));

        // Vector search — top 20
        var vectorResults = await baseQuery
            .Where(x => x.chunk.Embedding != null)
            .Select(x => new RetrievalCandidate
            {
                ChunkId = x.chunk.Id,
                DocumentId = x.doc.Id,
                FileName = x.doc.FileName,
                ChunkIndex = x.chunk.ChunkIndex,
                Content = x.chunk.Content,
                Score = 1 - x.chunk.Embedding!.CosineDistance(queryVector)
            })
            .OrderByDescending(x => x.Score)
            .Take(20)
            .ToListAsync(ct);

        // Keyword search — top 20
        List<RetrievalCandidate> keywordResults = [];
        if (!string.IsNullOrWhiteSpace(tsQueryText))
        {
            keywordResults = await baseQuery
                .Where(x => x.chunk.SearchVector != null &&
                            x.chunk.SearchVector.Matches(EF.Functions.ToTsQuery("english", tsQueryText)))
                .Select(x => new RetrievalCandidate
                {
                    ChunkId = x.chunk.Id,
                    DocumentId = x.doc.Id,
                    FileName = x.doc.FileName,
                    ChunkIndex = x.chunk.ChunkIndex,
                    Content = x.chunk.Content,
                    Score = x.chunk.SearchVector!.Rank(EF.Functions.ToTsQuery("english", tsQueryText))
                })
                .OrderByDescending(x => x.Score)
                .Take(20)
                .ToListAsync(ct);
        }

        // RRF merge
        var rrfMap = new Dictionary<string, RetrievalCandidate>();

        for (int i = 0; i < vectorResults.Count; i++)
        {
            var item = vectorResults[i];
            var key = $"{item.DocumentId}_{item.ChunkIndex}";
            item.VectorSimilarity = item.Score;
            item.Score = 1.0 / (RrfK + i + 1);
            rrfMap[key] = item;
        }

        for (int i = 0; i < keywordResults.Count; i++)
        {
            var item = keywordResults[i];
            var key = $"{item.DocumentId}_{item.ChunkIndex}";
            var kwScore = 1.0 / (RrfK + i + 1);
            if (rrfMap.TryGetValue(key, out var existing))
                existing.Score += kwScore;
            else
                rrfMap[key] = item with { Score = kwScore };
        }

        var top = rrfMap.Values
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();

        var vectorSimFirst = top.Count > 0 ? top[0].VectorSimilarity : 0.0;
        return (top, vectorSimFirst);
    }
}
