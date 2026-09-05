using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Normora.Api.Features.Ask;

public sealed record AskQuestionQuery(string Question, int Limit = 5) : IRequest<AskQuestionResult>;

public sealed record AskQuestionResult(
    string Answer,
    IReadOnlyList<AskCitation> Sources);

public sealed record AskCitation(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    double Similarity);

public sealed class AskQuestionQueryHandler(
    DocumentsDbContext context,
    Normora.Api.Features.Documents.ITextEmbeddingService embeddingService,
    ITextGenerationService generationService) : IRequestHandler<AskQuestionQuery, AskQuestionResult>
{
    private const double MinimumSimilarity = 0.35;

    public async Task<AskQuestionResult> Handle(
        AskQuestionQuery request,
        CancellationToken cancellationToken)
    {
        if (!embeddingService.IsConfigured || !generationService.IsConfigured)
        {
            throw new InvalidOperationException("Ask Normora requires Gemini embeddings and generation to be configured.");
        }

        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(request.Question, cancellationToken));
        var limit = Math.Clamp(request.Limit, 1, 8);

        // The global tenant filters apply to both chunks and documents before the vector
        // ranking, so the prompt can only contain the active tenant's source material.
        var sourceRows = await context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null)
            .Join(
                context.Documents.AsNoTracking(),
                chunk => chunk.DocumentId,
                document => document.Id,
                (chunk, document) => new
                {
                    DocumentId = document.Id,
                    document.FileName,
                    chunk.ChunkIndex,
                    chunk.Content,
                    Similarity = 1 - chunk.Embedding!.CosineDistance(queryVector)
                })
            .OrderByDescending(result => result.Similarity)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Documents uploaded without Gemini embeddings are not searchable yet. Treat an
        // empty result as a normal no-answer state instead of indexing sourceRows[0].
        if (sourceRows.Count == 0)
        {
            return new AskQuestionResult(
                "I could not find that in the company documents.",
                []);
        }

        if (sourceRows[0].Similarity < MinimumSimilarity)
        {
            return new AskQuestionResult(
                "I could not find that in the company documents.",
                []);
        }

        var sources = sourceRows
            .Select(row => new AskSource(row.FileName, row.ChunkIndex, row.Content))
            .ToList();
        var answer = await generationService.GenerateGroundedAnswerAsync(request.Question, sources, cancellationToken);

        return new AskQuestionResult(
            answer,
            sourceRows.Select(row => new AskCitation(
                row.DocumentId,
                row.FileName,
                row.ChunkIndex,
                row.Similarity)).ToList());
    }
}