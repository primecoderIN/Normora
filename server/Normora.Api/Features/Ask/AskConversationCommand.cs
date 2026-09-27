using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Api.Features.Documents;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Api.Features.Ask;

/// <summary>
/// Command to send a message within a conversation and receive a grounded, context-aware RAG answer.
/// 
/// Pipeline:
///   1. Validate conversation ownership
///   2. Persist user message + estimate its token count
///   3. Load conversation history, trim to token budget (sliding window)
///   4. Rewrite follow-up question → standalone retrieval query
///   5. Hybrid vector + keyword retrieval with RRF fusion
///   6. Multi-turn grounded generation (history injected into prompt)
///   7. Persist assistant message (citations, token count, rewrite metadata)
///   8. Auto-title the conversation on its first turn
/// </summary>
public sealed record AskConversationCommand(
    Guid ConversationId,
    string Question,
    int Limit = 5) : IRequest<AskConversationResult>;

public sealed record AskConversationResult(
    Guid ConversationId,
    Guid UserMessageId,
    Guid AssistantMessageId,
    string Answer,
    IReadOnlyList<AskConversationCitation> Sources);

public sealed record AskConversationCitation(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    double Score);

public sealed class AskConversationCommandHandler(
    ConversationsDbContext conversationsContext,
    Normora.Modules.Tenants.Persistence.TenantsDbContext tenantsContext,
    IContextResolver contextResolver,
    IQueryRewriterService queryRewriter,
    ITokenBudgetService tokenBudget,
    IRetrievalService retrievalService,
    ITextGenerationService generationService,
    IAutoTitleService autoTitleService,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IMeterFactory meterFactory) : IRequestHandler<AskConversationCommand, AskConversationResult>
{
    private const double MinimumSimilarity = 0.35;

    private readonly Counter<long> _tokensConsumed = meterFactory.Create("Normora.Conversations").CreateCounter<long>("tokens.consumed", description: "Estimated LLM tokens consumed");
    private readonly Histogram<double> _ragDuration = meterFactory.Create("Normora.Conversations").CreateHistogram<double>("rag.duration", unit: "ms", description: "RAG pipeline execution latency");
    private readonly Counter<long> _autoTitleOperations = meterFactory.Create("Normora.Conversations").CreateCounter<long>("auto_title.operations", description: "Auto-title generation attempts (success or failure)");

    /// <summary>
    /// Maximum tokens allocated to conversation history injected into the generation prompt.
    /// Keeps the combined prompt well within Gemini Flash's context window.
    /// </summary>
    private const int HistoryTokenBudget = 2_000;

    public async Task<AskConversationResult> Handle(
        AskConversationCommand request,
        CancellationToken cancellationToken)
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
        // Load the last 20 messages (10 turns) as the raw window, then trim to budget.
        var rawHistory = await contextResolver.GetConversationContextAsync(
            request.ConversationId,
            currentUser.KeycloakUserId!,
            messageLimit: 20,
            cancellationToken: cancellationToken);

        // Exclude the user message we just persisted (it's the last item)
        var historyWithoutLatest = rawHistory.Count > 0
            ? rawHistory.Take(rawHistory.Count - 1).ToList()
            : [];

        // Apply token budget: keep most-recent messages that fit within 2000 tokens
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

        // ── 6. Multi-turn grounded generation ─────────────────────────────────
        string answerText;
        List<MessageCitation> citations = [];

        if (topCandidates.Count == 0 ||
            (vectorSimOfFirst > 0 && vectorSimOfFirst < MinimumSimilarity))
        {
            answerText = RagConstants.FallbackAnswer;
        }
        else
        {
            var sources = topCandidates
                .Select(c => new AskSource(c.FileName, c.ChunkIndex, c.Content))
                .ToList();

            // Build paired conversation turns for the generation prompt
            var historyTurns = AskHelpers.BuildConversationTurns(budgetedHistory);

            answerText = await generationService.GenerateConversationalAnswerAsync(
                rewrittenQuestion, sources, historyTurns, activeTenantName, cancellationToken);

            // ── 7a. Build citations ───────────────────────────────────────────
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
        }

        // ── 7b. Persist assistant message + citations ─────────────────────────
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
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

        // ── 8. Auto-title on first turn ───────────────────────────────────────
        // Fire-and-forget after the main save so latency is not impacted.
        // We only title once: when the conversation still has the default name.
        if (isFirstTurn && conversation.Title == ConversationConstants.DefaultTitle)
        {
            autoTitleService.AutoTitleConversationAsync(conversation.Id, request.Question);
        }

        _tokensConsumed.Add((userMessage.TokenCount ?? 0) + (assistantMessage.TokenCount ?? 0), new KeyValuePair<string, object?>("operation", "ask"));

        return new AskConversationResult(
            request.ConversationId,
            userMessage.Id,
            assistantMessage.Id,
            answerText,
            topCandidates.Select((c, idx) => new AskConversationCitation(
                c.DocumentId,
                c.FileName,
                c.ChunkIndex,
                c.Score)).ToList());
        }
        finally
        {
            stopwatch.Stop();
            _ragDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("operation", "ask"));
        }
    }

}
