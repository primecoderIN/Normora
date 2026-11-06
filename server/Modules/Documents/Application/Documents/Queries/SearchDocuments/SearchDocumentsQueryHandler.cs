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
        if (!embeddingService.IsConfigured)
        {
            throw new InvalidOperationException(ApiMessages.EmbeddingsNotConfigured);
        }

        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(request.Query, cancellationToken));
        var limit = Math.Clamp(request.Limit, 1, 20);
        var isAdmin = string.Equals(tenantContext.TenantRole, TenantRoles.Admin, StringComparison.OrdinalIgnoreCase);
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

