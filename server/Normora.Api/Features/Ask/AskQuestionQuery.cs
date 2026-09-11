using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Normora.Api.Features.Ask;

/// <summary>
/// Query to answer an employee's question using RAG (Retrieval-Augmented Generation).
/// Retrieves the most semantically relevant document chunks and passes them to the LLM for grounded answering.
/// </summary>
/// <param name="Question">The employee's natural language question.</param>
/// <param name="Limit">Maximum number of document chunks to retrieve (1–8).</param>
public sealed record AskQuestionQuery(string Question, int Limit = 5) : IRequest<AskQuestionResult>;

/// <summary>
/// Represents the result of an Ask Normora query, containing the generated answer and citations.
/// </summary>
public sealed record AskQuestionResult(
    string Answer,
    IReadOnlyList<AskCitation> Sources);

/// <summary>
/// Represents a source document chunk that contributed to the generated answer.
/// </summary>
public sealed record AskCitation(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    double Similarity);

/// <summary>
/// Handles <see cref="AskQuestionQuery"/> by performing a vector similarity search scoped to the user's
/// effective departments, then generating a grounded answer from the top-ranked chunks.
/// </summary>
public sealed class AskQuestionQueryHandler(
    DocumentsDbContext context,
    Normora.Api.Features.Documents.ITextEmbeddingService embeddingService,
    ITextGenerationService generationService,
    Normora.Shared.Interfaces.ITenantContext tenantContext) : IRequestHandler<AskQuestionQuery, AskQuestionResult>
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

        // Parse question for keyword search (OR logic)
        var queryWords = request.Question.Split([' ', '\t', '\n', '\r', '.', ',', '?', '!', '\''], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToArray();
        var tsQueryText = queryWords.Length > 0 ? string.Join(" | ", queryWords) : null;

        var effectiveDepartments = tenantContext.EffectiveDepartments;

        var baseQuery = context.DocumentChunks
            .AsNoTracking()
            .Join(
                context.Documents.AsNoTracking().Where(d => 
                    !d.DocumentDepartments.Any() || 
                    d.DocumentDepartments.Any(dd => effectiveDepartments.Contains(dd.DepartmentId))), 
                chunk => chunk.DocumentId,
                document => document.Id,
                (chunk, document) => new { chunk, document });

        // 1. Vector Search (Top 20)
        var vectorResults = await baseQuery
            .Where(x => x.chunk.Embedding != null)
            .Select(x => new
            {
                DocumentId = x.document.Id,
                x.document.FileName,
                x.chunk.ChunkIndex,
                x.chunk.Content,
                Score = 1 - x.chunk.Embedding!.CosineDistance(queryVector)
            })
            .OrderByDescending(x => x.Score)
            .Take(20)
            .ToListAsync(cancellationToken);

        // 2. Keyword Search (Top 20)
        var keywordResults = new List<dynamic>();
        if (!string.IsNullOrWhiteSpace(tsQueryText))
        {
            var keywordQuery = await baseQuery
                .Where(x => x.chunk.SearchVector != null && x.chunk.SearchVector.Matches(EF.Functions.ToTsQuery("english", tsQueryText)))
                .Select(x => new
                {
                    DocumentId = x.document.Id,
                    x.document.FileName,
                    x.chunk.ChunkIndex,
                    x.chunk.Content,
                    Score = x.chunk.SearchVector!.Rank(EF.Functions.ToTsQuery("english", tsQueryText))
                })
                .OrderByDescending(x => x.Score)
                .Take(20)
                .ToListAsync(cancellationToken);
            keywordResults.AddRange(keywordQuery);
        }

        // 3. Reciprocal Rank Fusion (RRF)
        var rrfScores = new Dictionary<string, (double Score, string FileName, Guid DocumentId, int ChunkIndex, string Content, double VectorSimilarity)>();
        const double k = 60.0;

        for (int i = 0; i < vectorResults.Count; i++)
        {
            var item = vectorResults[i];
            var key = $"{item.DocumentId}_{item.ChunkIndex}";
            rrfScores[key] = (1.0 / (k + i + 1), item.FileName, item.DocumentId, item.ChunkIndex, item.Content, item.Score);
        }

        for (int i = 0; i < keywordResults.Count; i++)
        {
            var item = keywordResults[i];
            var key = $"{item.DocumentId}_{item.ChunkIndex}";
            var keywordScore = 1.0 / (k + i + 1);
            
            if (rrfScores.TryGetValue(key, out var existing))
            {
                rrfScores[key] = (existing.Score + keywordScore, existing.FileName, existing.DocumentId, existing.ChunkIndex, existing.Content, existing.VectorSimilarity);
            }
            else
            {
                rrfScores[key] = (keywordScore, item.FileName, item.DocumentId, item.ChunkIndex, item.Content, 0.0);
            }
        }

        var topCandidates = rrfScores.Values
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();

        if (topCandidates.Count == 0 || (topCandidates[0].VectorSimilarity > 0 && topCandidates[0].VectorSimilarity < MinimumSimilarity))
        {
            return new AskQuestionResult(
                "I could not find that in the company documents.",
                []);
        }

        var sources = topCandidates
            .Select(row => new AskSource(row.FileName, row.ChunkIndex, row.Content))
            .ToList();
            
        var answer = await generationService.GenerateGroundedAnswerAsync(request.Question, sources, cancellationToken);

        return new AskQuestionResult(
            answer,
            topCandidates.Select(row => new AskCitation(
                row.DocumentId,
                row.FileName,
                row.ChunkIndex,
                row.Score)).ToList());
    }
}