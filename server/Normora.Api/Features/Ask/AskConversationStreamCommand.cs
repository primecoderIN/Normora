using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Normora.Api.Features.Documents;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Normora.Api.Features.Ask;

/// <summary>
/// Streaming variant of <see cref="AskConversationCommand"/>. Same RAG pipeline but yields
/// <see cref="AskConversationStreamEvent"/> instances via SSE instead of returning a single result.
/// </summary>
public sealed record AskConversationStreamCommand(
    Guid ConversationId,
    string Question,
    int Limit = 5) : IStreamRequest<AskConversationStreamEvent>;

public sealed class AskConversationStreamCommandHandler(
    ConversationsDbContext conversationsContext,
    DocumentsDbContext documentsContext,
    IContextResolver contextResolver,
    IQueryRewriterService queryRewriter,
    ITokenBudgetService tokenBudget,
    ITextEmbeddingService embeddingService,
    ITextGenerationService generationService,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IServiceScopeFactory scopeFactory,
    IMeterFactory meterFactory) : IStreamRequestHandler<AskConversationStreamCommand, AskConversationStreamEvent>
{
    private const double MinimumSimilarity = 0.0;
    private const double RrfK = 60.0;
    private const int HistoryTokenBudget = 2_000;

    private readonly Counter<long> _tokensConsumed = meterFactory.Create("Normora.Conversations").CreateCounter<long>("tokens.consumed", description: "Estimated LLM tokens consumed");
    private readonly Histogram<double> _ragDuration = meterFactory.Create("Normora.Conversations").CreateHistogram<double>("rag.duration", unit: "ms", description: "RAG pipeline execution latency");
    private readonly Counter<long> _autoTitleOperations = meterFactory.Create("Normora.Conversations").CreateCounter<long>("auto_title.operations", description: "Auto-title generation attempts (success or failure)");

    public async IAsyncEnumerable<AskConversationStreamEvent> Handle(
        AskConversationStreamCommand request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!generationService.IsConfigured)
            throw new InvalidOperationException("Ask Normora requires Gemini generation to be configured.");

        var stopwatch = Stopwatch.StartNew();
        try
        {

        // ── 1. Validate conversation ownership ────────────────────────────────
        var conversation = await conversationsContext.Conversations
            .FirstOrDefaultAsync(c =>
                c.Id == request.ConversationId &&
                c.UserId == currentUser.KeycloakUserId,
                cancellationToken)
            ?? throw new InvalidOperationException("Conversation not found or access denied.");

        var isFirstTurn = !await conversationsContext.Messages
            .AnyAsync(m => m.ConversationId == request.ConversationId, cancellationToken);

        // ── 2. Persist user message immediately ───────────────────────────────
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            TenantId = tenantContext.TenantId!.Value,
            Role = MessageRole.User,
            Content = request.Question,
            CreatedAt = DateTimeOffset.UtcNow,
            TokenCount = tokenBudget.EstimateTokenCount(request.Question)
        };
        conversationsContext.Messages.Add(userMessage);
        await conversationsContext.SaveChangesAsync(cancellationToken);

        // ── 3. Load history + apply sliding-window token budget ───────────────
        var rawHistory = await contextResolver.GetConversationContextAsync(
            request.ConversationId,
            currentUser.KeycloakUserId!,
            messageLimit: 20,
            cancellationToken: cancellationToken);

        var historyWithoutLatest = rawHistory.Count > 0
            ? rawHistory.Take(rawHistory.Count - 1).ToList()
            : [];

        var budgetedHistory = tokenBudget.TrimToTokenBudget(historyWithoutLatest, HistoryTokenBudget);

        // ── 4. Rewrite follow-up into standalone retrieval query ──────────────
        var rewrittenQuestion = budgetedHistory.Count > 0
            ? await queryRewriter.RewriteQueryAsync(request.Question, budgetedHistory, cancellationToken)
            : request.Question;
        var wasRewritten = rewrittenQuestion != request.Question;

        // ── 5. Hybrid retrieval (vector + keyword + RRF) ──────────────────────
        var limit = Math.Clamp(request.Limit, 1, 8);
        var (topCandidates, vectorSimOfFirst) =
            await RunHybridRetrievalAsync(rewrittenQuestion, limit, cancellationToken);

        // ── 6. Build citations + emit immediately ─────────────────────────────
        var assistantMessageId = Guid.NewGuid();
        var citations = new List<MessageCitation>();
        var mappedSources = new List<AskConversationCitation>();

        if (topCandidates.Count > 0 && (vectorSimOfFirst == 0 || vectorSimOfFirst >= MinimumSimilarity))
        {
            citations = topCandidates.Select((c, idx) => new MessageCitation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId!.Value,
                DocumentId = c.DocumentId,
                DocumentChunkId = c.ChunkId,
                FileName = c.FileName,
                Rank = idx + 1,
                Score = c.Score,
                RetrievalMethod = RetrievalMethod.Hybrid
            }).ToList();

            mappedSources = topCandidates.Select(c => new AskConversationCitation(
                c.DocumentId,
                c.FileName,
                c.ChunkIndex,
                c.Score)).ToList();
        }

        // Emit citations before streaming text
        yield return new CitationsReadyEvent(userMessage.Id, assistantMessageId, mappedSources);

        // ── 7. Stream generation ──────────────────────────────────────────────
        var answerBuilder = new System.Text.StringBuilder();

        if (topCandidates.Count == 0 || (vectorSimOfFirst > 0 && vectorSimOfFirst < MinimumSimilarity))
        {
            var text = "I could not find that in the company documents.";
            answerBuilder.Append(text);
            yield return new TextChunkEvent(text);
        }
        else
        {
            var sources = topCandidates
                .Select(c => new AskSource(c.FileName, c.ChunkIndex, c.Content))
                .ToList();

            var historyTurns = BuildConversationTurns(budgetedHistory);

            await foreach (var chunk in generationService.StreamConversationalAnswerAsync(
                rewrittenQuestion, sources, historyTurns, cancellationToken))
            {
                answerBuilder.Append(chunk);
                yield return new TextChunkEvent(chunk);
            }
        }

        var answerText = answerBuilder.ToString();

        // ── 8. Persist assistant message + citations ──────────────────────────
        var assistantMessage = new Message
        {
            Id = assistantMessageId,
            ConversationId = request.ConversationId,
            TenantId = tenantContext.TenantId!.Value,
            Role = MessageRole.Assistant,
            Content = answerText,
            CreatedAt = DateTimeOffset.UtcNow,
            TokenCount = tokenBudget.EstimateTokenCount(answerText),
            RetrievalQuery = wasRewritten ? rewrittenQuestion : null,
            Rewritten = wasRewritten,
            RequiresContext = true
        };

        foreach (var citation in citations)
        {
            citation.MessageId = assistantMessage.Id;
            assistantMessage.Citations.Add(citation);
        }

        conversationsContext.Messages.Add(assistantMessage);

        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;

        await conversationsContext.SaveChangesAsync(cancellationToken);

        // ── 9. Auto-title on first turn ───────────────────────────────────────
        if (isFirstTurn && conversation.Title == "New conversation")
        {
            AutoTitleConversationAsync(conversation.Id, request.Question);
        }

        _tokensConsumed.Add((userMessage.TokenCount ?? 0) + (assistantMessage.TokenCount ?? 0), new KeyValuePair<string, object?>("operation", "ask"));

        yield return new StreamFinishedEvent();
        
        }
        finally
        {
            stopwatch.Stop();
            _ragDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("operation", "ask"));
        }
    }

    // ─── Hybrid Retrieval ────────────────────────────────────────────────────────

    private async Task<(List<RetrievalCandidate> Candidates, double VectorSimOfFirst)>
        RunHybridRetrievalAsync(string question, int limit, CancellationToken ct)
    {
        var queryVector = new Vector(await embeddingService.CreateEmbeddingAsync(question, ct));
        var effectiveDepartments = tenantContext.EffectiveDepartments;

        var queryWords = question.Split([' ', '\t', '\n', '\r', '.', ',', '?', '!', '\''],
            StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length > 2).ToArray();
        var tsQueryText = queryWords.Length > 0 ? string.Join(" | ", queryWords) : null;

        var baseQuery = documentsContext.DocumentChunks
            .AsNoTracking()
            .Join(
                documentsContext.Documents.AsNoTracking().Where(d =>
                    !d.DocumentDepartments.Any() ||
                    d.DocumentDepartments.Any(dd => effectiveDepartments.Contains(dd.DepartmentId))),
                chunk => chunk.DocumentId,
                doc => doc.Id,
                (chunk, doc) => new { chunk, doc });

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

    private static List<ConversationTurn> BuildConversationTurns(
        IReadOnlyList<ConversationMessageContext> history)
    {
        var turns = new List<ConversationTurn>();
        for (int i = 0; i + 1 < history.Count; i += 2)
        {
            var a = history[i];
            var b = history[i + 1];
            if (string.Equals(a.Role, MessageRole.User.ToString(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Role, MessageRole.Assistant.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                turns.Add(new ConversationTurn(a.Content, b.Content));
            }
        }
        return turns;
    }

    private void AutoTitleConversationAsync(Guid conversationId, string firstQuestion)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ConversationsDbContext>();
                var gen = scope.ServiceProvider.GetRequiredService<ITextGenerationService>();

                var title = await gen.GenerateTitleAsync(firstQuestion);
                if (string.IsNullOrWhiteSpace(title)) return;

                var conv = await db.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);
                if (conv is null || conv.Title != "New conversation") return;

                conv.Title = title;
                conv.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();

                _autoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "success"));
            }
            catch
            {
                _autoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "failure"));
            }
        });
    }

    private sealed record RetrievalCandidate
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public double VectorSimilarity { get; set; }
    }
}
