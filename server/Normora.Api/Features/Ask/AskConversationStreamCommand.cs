using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Api.Features.Documents;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

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
    TenantsDbContext tenantsContext,
    IContextResolver contextResolver,
    IQueryRewriterService queryRewriter,
    ITokenBudgetService tokenBudget,
    IRetrievalService retrievalService,
    ITextGenerationService generationService,
    IAutoTitleService autoTitleService,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ConversationMetrics metrics) : IStreamRequestHandler<AskConversationStreamCommand, AskConversationStreamEvent>
{
    private const double MinimumSimilarity = 0.35;
    private const int HistoryTokenBudget = 2_000;


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
            ?? throw new Normora.Shared.Exceptions.BolaException();

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
        
        // Find the user's personal tenant so we can include it in the retrieval
        var userMemberships = await tenantsContext.TenantMemberships
            .Include(m => m.Tenant)
            .Where(m => m.User.KeycloakUserId == currentUser.KeycloakUserId)
            .ToListAsync(cancellationToken);
            
        var personalTenantId = userMemberships.FirstOrDefault(m => m.Tenant.IsPersonal)?.TenantId;
        var activeTenantName = userMemberships.FirstOrDefault(m => m.TenantId == tenantContext.TenantId)?.Tenant.Name ?? "your organization";

        var (topCandidates, vectorSimOfFirst) =
            await retrievalService.RunHybridRetrievalAsync(rewrittenQuestion, limit, personalTenantId, cancellationToken);

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
            var text = RagConstants.FallbackAnswer;
            answerBuilder.Append(text);
            yield return new TextChunkEvent(text);
        }
        else
        {
            var sources = topCandidates
                .Select(c => new AskSource(c.FileName, c.ChunkIndex, c.Content))
                .ToList();

            var historyTurns = AskHelpers.BuildConversationTurns(budgetedHistory);

            await foreach (var chunk in generationService.StreamConversationalAnswerAsync(
                rewrittenQuestion, sources, historyTurns, activeTenantName, cancellationToken))
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
        if (isFirstTurn && conversation.Title == ConversationConstants.DefaultTitle)
        {
            autoTitleService.AutoTitleConversationAsync(conversation.Id, request.Question);
        }

        metrics.TokensConsumed.Add((userMessage.TokenCount ?? 0) + (assistantMessage.TokenCount ?? 0), new KeyValuePair<string, object?>("operation", "ask"));

        yield return new StreamFinishedEvent();
        
        }
        finally
        {
            stopwatch.Stop();
            metrics.RagDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("operation", "ask"));
        }
    }

    // ─── Hybrid Retrieval ────────────────────────────────────────────────────────

}
