using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Normora.Modules.Documents.Application.Documents;

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
/// effective department visibility.
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

        return await context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null)
            // Document Versioning: Only retrieve chunks from the active document version.
            .Join(
                context.DocumentVersions.AsNoTracking().Where(v => v.IsActive),
                chunk => chunk.DocumentVersionId,
                version => version.Id,
                (chunk, version) => new { chunk, version }
            )
            .Join(
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
