using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Pgvector.EntityFrameworkCore;
using Pgvector;

namespace Normora.Modules.Documents.Application.Services;

/// <summary>
/// Executes hybrid retrieval combining pgvector dense embeddings with PostgreSQL full-text search,
/// merged via Reciprocal Rank Fusion (RRF) for optimal context grounding in RAG queries.
/// </summary>
public sealed class RetrievalService(
    DocumentsDbContext documentsContext,
    ITenantContext tenantContext,
    ITextEmbeddingService embeddingService) : IRetrievalService
{
    // Standard RRF smoothing constant to balance dense and sparse rank distributions
    private const double RrfK = 60.0;

    public async Task<(List<RetrievalCandidate> Candidates, double VectorSimOfFirst)>
        RunHybridRetrievalAsync(string question, int limit, Guid? personalTenantId, CancellationToken ct = default)
    {
        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(question, ct));
        var isAdmin = string.Equals(tenantContext.TenantRole, TenantRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var effectiveDepartments = tenantContext.EffectiveDepartments;
        var activeTenantId = tenantContext.TenantId;

        // Base query joins chunks with active versions and applies tenant + department RBAC visibility filters
        var baseQuery = documentsContext.DocumentChunks
            .IgnoreQueryFilters()
            .AsNoTracking()
            // Only retrieve from active versions to exclude superseded document revisions
            .Join(
                documentsContext.DocumentVersions.IgnoreQueryFilters().AsNoTracking().Where(v => v.IsActive),
                chunk => chunk.DocumentVersionId,
                version => version.Id,
                (chunk, version) => new { chunk, version })
            .Join(
                documentsContext.Documents.IgnoreQueryFilters().AsNoTracking().Where(d =>
                    isAdmin ||
                    !d.DocumentDepartments.Any() ||
                    d.DocumentDepartments.Any(dd => effectiveDepartments.Contains(dd.DepartmentId))),
                cv => cv.version.DocumentId,
                doc => doc.Id,
                (cv, doc) => new { cv.chunk, cv.version, doc })
            .Where(x => (x.chunk.TenantId == activeTenantId || (personalTenantId.HasValue && x.chunk.TenantId == personalTenantId.Value))
                     && x.doc.DeletedAt == null);

        // Vector search — top 20 candidates with baseline similarity threshold (cosine distance <= 0.70 => similarity >= 0.30)
        var vectorResults = await baseQuery
            .Where(x => x.chunk.Embedding != null)
            .Select(x => new
            {
                ChunkId = x.chunk.Id,
                DocumentId = x.doc.Id,
                FileName = x.doc.FileName,
                ChunkIndex = x.chunk.ChunkIndex,
                Content = x.chunk.Content,
                Distance = x.chunk.Embedding!.CosineDistance(queryVector)
            })
            .Where(x => x.Distance <= 0.70)
            .OrderBy(x => x.Distance)
            .Take(20)
            .Select(x => new RetrievalCandidate
            {
                ChunkId = x.ChunkId,
                DocumentId = x.DocumentId,
                FileName = x.FileName,
                ChunkIndex = x.ChunkIndex,
                Content = x.Content,
                Score = Math.Max(0.0, 1.0 - x.Distance),
                VectorSimilarity = Math.Max(0.0, 1.0 - x.Distance)
            })
            .ToListAsync(ct);

        // Keyword search — top 20 candidates using PostgreSQL plainto_tsquery (strips stop words & stems lexemes)
        List<RetrievalCandidate> keywordResults = [];
        if (!string.IsNullOrWhiteSpace(question))
        {
            try
            {
                keywordResults = await baseQuery
                    .Where(x => x.chunk.SearchVector != null &&
                                x.chunk.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", question)))
                    .Select(x => new
                    {
                        ChunkId = x.chunk.Id,
                        DocumentId = x.doc.Id,
                        FileName = x.doc.FileName,
                        ChunkIndex = x.chunk.ChunkIndex,
                        Content = x.chunk.Content,
                        Rank = x.chunk.SearchVector!.Rank(EF.Functions.PlainToTsQuery("english", question)),
                        Distance = x.chunk.Embedding != null ? x.chunk.Embedding.CosineDistance(queryVector) : 1.0
                    })
                    .OrderByDescending(x => x.Rank)
                    .Take(20)
                    .Select(x => new RetrievalCandidate
                    {
                        ChunkId = x.ChunkId,
                        DocumentId = x.DocumentId,
                        FileName = x.FileName,
                        ChunkIndex = x.ChunkIndex,
                        Content = x.Content,
                        Score = x.Rank,
                        VectorSimilarity = Math.Max(0.0, 1.0 - x.Distance)
                    })
                    .ToListAsync(ct);
            }
            catch (Exception)
            {
                // Graceful fallback to dense vector search if tsquery encounters an unexpected dialect issue
                keywordResults = [];
            }
        }

        // Merge dense vector and sparse keyword result sets using Reciprocal Rank Fusion: RrfScore = sum(1 / (k + rank))
        var rrfMap = new Dictionary<string, (RetrievalCandidate Candidate, double RrfScore)>();

        for (int i = 0; i < vectorResults.Count; i++)
        {
            var item = vectorResults[i];
            var key = $"{item.DocumentId}_{item.ChunkIndex}";
            var rrfScore = 1.0 / (RrfK + i + 1);
            rrfMap[key] = (item, rrfScore);
        }

        for (int i = 0; i < keywordResults.Count; i++)
        {
            var item = keywordResults[i];
            var key = $"{item.DocumentId}_{item.ChunkIndex}";
            var kwRrfScore = 1.0 / (RrfK + i + 1);
            if (rrfMap.TryGetValue(key, out var existing))
            {
                var combinedVectorSim = Math.Max(existing.Candidate.VectorSimilarity, item.VectorSimilarity);
                rrfMap[key] = (existing.Candidate with { VectorSimilarity = combinedVectorSim }, existing.RrfScore + kwRrfScore);
            }
            else
            {
                rrfMap[key] = (item, kwRrfScore);
            }
        }

        // Order candidates by fused RRF rank, then calibrate final user-facing relevance Score in [0.0, 1.0]
        var top = rrfMap.Values
            .OrderByDescending(entry => entry.RrfScore)
            .Take(limit)
            .Select(entry =>
            {
                var candidate = entry.Candidate;
                double relevance;

                if (candidate.VectorSimilarity > 0)
                {
                    // If corroborated by both dense vector and sparse keyword search, apply a 5% confidence boost
                    var isHybridConfirmed = entry.RrfScore > (1.0 / (RrfK + 1));
                    relevance = isHybridConfirmed
                        ? Math.Min(1.0, candidate.VectorSimilarity * 1.05)
                        : candidate.VectorSimilarity;
                }
                else
                {
                    // Sparse keyword match without vector embedding: normalize rank score
                    relevance = Math.Clamp(candidate.Score * 2.0, 0.40, 0.85);
                }

                candidate.Score = Math.Round(Math.Clamp(relevance, 0.0, 1.0), 4);
                return candidate;
            })
            .ToList();

        var vectorSimFirst = top.Count > 0 ? top[0].VectorSimilarity : 0.0;
        return (top, vectorSimFirst);
    }
}
