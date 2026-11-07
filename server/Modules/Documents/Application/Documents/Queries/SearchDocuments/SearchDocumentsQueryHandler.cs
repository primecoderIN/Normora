using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Application.Documents.DTOs;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Normora.Modules.Documents.Application.Documents.Queries;

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
        // Provider Configuration Check:
        // Validates that Google Gemini text-embedding-004 API credentials are configured in host settings.
        if (!embeddingService.IsConfigured)
        {
            throw new InvalidOperationException(ApiMessages.EmbeddingsNotConfigured);
        }

        // Query Vector Generation:
        // Converts the natural language query into a 768-dimensional normalized float vector.
        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(request.Query, cancellationToken));
        var limit = Math.Clamp(request.Limit, 1, 20);
        var isAdmin = string.Equals(tenantContext.TenantRole, TenantRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var effectiveDepartments = tenantContext.EffectiveDepartments;

        // Departmental ACL & Active Version Vector Cosine Search:
        // Joins DocumentChunks with DocumentVersions and Documents with three strict security filters:
        // 1. IsActive == true: Prevents matching chunks from superseded or historical document versions.
        // 2. Departmental Authorization:
        //    - If the user is an Admin, they have full visibility across all documents.
        //    - If the document has no department restrictions (!d.DocumentDepartments.Any()), it is accessible company-wide.
        //    - Otherwise, the caller must possess at least one matching department in effectiveDepartments (direct or via groups).
        // 3. CosineDistance: Calculates 1 - cosine_similarity via pgvector HNSW index.
        return await context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null)
            .Join(
                context.DocumentVersions.AsNoTracking().Where(v => v.IsActive),
                chunk => chunk.DocumentVersionId,
                version => version.Id,
                (chunk, version) => new { chunk, version }
            )
            .Join(
                context.Documents.AsNoTracking().Where(d =>
                    isAdmin ||
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

