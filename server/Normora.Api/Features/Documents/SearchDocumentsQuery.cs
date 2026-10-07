using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Query to perform a semantic (vector) similarity search across all indexed document chunks for the current tenant.
/// </summary>
/// <param name="Query">The search text to embed and compare against stored chunk embeddings.</param>
/// <param name="Limit">Maximum number of results to return (1–20).</param>
public sealed record SearchDocumentsQuery(string Query, int Limit = 5) : IRequest<IReadOnlyList<DocumentSearchResult>>;

/// <summary>
/// Represents a single document chunk returned from a semantic search.
/// </summary>
public sealed record DocumentSearchResult(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    double Similarity);

/// <summary>
/// Handles <see cref="SearchDocumentsQuery"/> by performing a cosine similarity search on pgvector embeddings,
/// restricted to the current tenant's documents via a global EF Core query filter AND the caller's
/// effective department visibility — mirroring the authorization policy applied in RAG retrieval.
/// </summary>
public sealed class SearchDocumentsQueryHandler(
    DocumentsDbContext context,
    ITextEmbeddingService embeddingService,
    ITenantContext tenantContext) : IRequestHandler<SearchDocumentsQuery, IReadOnlyList<DocumentSearchResult>>
{
    public async Task<IReadOnlyList<DocumentSearchResult>> Handle(
        SearchDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!embeddingService.IsConfigured)
        {
            throw new InvalidOperationException(ApiMessages.EmbeddingsNotConfigured);
        }

        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(request.Query, cancellationToken));
        var limit = Math.Clamp(request.Limit, 1, 20);
        var effectiveDepartments = tenantContext.EffectiveDepartments;

        // The global tenant filter applies before this ranking query, so similarity search
        // cannot compare the current user's question with another tenant's chunks.
        // SEC-1: Additionally apply department-visibility to match the authorization policy
        // enforced in RAG retrieval (RetrievalService). Documents with no department
        // assignments are "company-wide" and visible to all tenant members. Documents
        // assigned to specific departments are only returned when the caller's
        // EffectiveDepartments overlap — preventing cross-department content leakage.
        return await context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null)
            // Document Versioning (Phase 19): Only retrieve chunks from the active document version.
            // Inactive versions (superseded by newer uploads) are excluded from search results.
            .Join(
                context.DocumentVersions.AsNoTracking().Where(v => v.IsActive),
                chunk => chunk.DocumentVersionId,
                version => version.Id,
                (chunk, version) => new { chunk, version }
            )
            .Join(
                // SEC-1: Mirror department-visibility policy from RetrievalService.
                // !Any() = no department restrictions (company-wide) → always visible.
                // Any(dd => ...) = restricted to specific departments → caller must be a member.
                context.Documents.AsNoTracking().Where(d =>
                    !d.DocumentDepartments.Any() ||
                    d.DocumentDepartments.Any(dd => effectiveDepartments.Contains(dd.DepartmentId))),
                cv => cv.version.DocumentId,
                document => document.Id,
                (cv, document) => new
                {
                    Chunk = cv.chunk,
                    Version = cv.version,
                    Document = document,
                    Distance = cv.chunk.Embedding!.CosineDistance(queryVector)
                })
            .OrderBy(result => result.Distance)
            .Take(limit)
            .Select(result => new DocumentSearchResult(
                result.Document.Id,
                result.Document.FileName,
                result.Chunk.ChunkIndex,
                result.Chunk.Content,
                1 - result.Distance))
            .ToListAsync(cancellationToken);
    }
}