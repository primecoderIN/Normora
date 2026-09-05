using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Normora.Api.Features.Documents;

public sealed record SearchDocumentsQuery(string Query, int Limit = 5) : IRequest<IReadOnlyList<DocumentSearchResult>>;

public sealed record DocumentSearchResult(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    double Similarity);

public sealed class SearchDocumentsQueryHandler(
    DocumentsDbContext context,
    ITextEmbeddingService embeddingService) : IRequestHandler<SearchDocumentsQuery, IReadOnlyList<DocumentSearchResult>>
{
    public async Task<IReadOnlyList<DocumentSearchResult>> Handle(
        SearchDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!embeddingService.IsConfigured)
        {
            throw new InvalidOperationException("Document search requires Gemini embeddings to be configured.");
        }

        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(request.Query, cancellationToken));
        var limit = Math.Clamp(request.Limit, 1, 20);

        // The global tenant filter applies before this ranking query, so similarity search
        // cannot compare the current user's question with another tenant's chunks.
        return await context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null)
            .Join(
                context.Documents.AsNoTracking(),
                chunk => chunk.DocumentId,
                document => document.Id,
                (chunk, document) => new
                {
                    Chunk = chunk,
                    Document = document,
                    Distance = chunk.Embedding!.CosineDistance(queryVector)
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