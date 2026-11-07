using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Contracts.Tenants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Encapsulates the shared RAG turn preparation pipeline: validates conversation ownership,
/// persists the user message, builds budgeted history, rewrites queries, and performs hybrid retrieval.
/// Eliminates code duplication across synchronous and streaming turn handlers.
/// </summary>
internal sealed class ConversationTurnPreparer(
    ConversationsDbContext conversationsContext,
    IMediator mediator,
    IContextResolver contextResolver,
    IQueryRewriterService queryRewriter,
    ITokenBudgetService tokenBudget,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : IConversationTurnPreparer
{
    private const int HistoryTokenBudget = 2_000;

    public async Task<ConversationTurnContext> PrepareTurnAsync(
        Guid conversationId,
        string question,
        int limit,
        CancellationToken cancellationToken)
    {
        // 1. Validate conversation ownership (BOLA check)
        var conversation = await conversationsContext.Conversations
            .FirstOrDefaultAsync(c =>
                c.Id == conversationId &&
                c.UserId == currentUser.ExternalUserId,
                cancellationToken)
            ?? throw new BolaException();

        var isFirstTurn = !await conversationsContext.Messages
            .AnyAsync(m => m.ConversationId == conversationId, cancellationToken);

        // 2. Persist user message immediately
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            TenantId = tenantContext.TenantId!.Value,
            Role = MessageRole.User,
            Content = question,
            CreatedAt = DateTimeOffset.UtcNow,
            TokenCount = tokenBudget.EstimateTokenCount(question)
        };
        conversationsContext.Messages.Add(userMessage);
        await conversationsContext.SaveChangesAsync(cancellationToken);

        // 3. Load history + apply sliding-window token budget
        var rawHistory = await contextResolver.GetConversationContextAsync(
            conversationId,
            currentUser.ExternalUserId,
            messageLimit: 20,
            cancellationToken: cancellationToken);

        var historyWithoutLatest = rawHistory.Count > 0
            ? rawHistory.Take(rawHistory.Count - 1).ToList()
            : [];

        var budgetedHistory = tokenBudget.TrimToTokenBudget(historyWithoutLatest, HistoryTokenBudget);

        // 4. Rewrite follow-up into standalone retrieval query
        var rewrittenQuestion = budgetedHistory.Count > 0
            ? await queryRewriter.RewriteQueryAsync(question, budgetedHistory, cancellationToken)
            : question;
        var wasRewritten = rewrittenQuestion != question;

        // 5. Hybrid retrieval (vector + keyword + RRF)
        var clampedLimit = Math.Clamp(limit, 1, 8);

        var workspaceContext = await mediator.Send(
            new GetUserWorkspaceContextQuery(
                currentUser.ExternalUserId,
                tenantContext.TenantId!.Value),
            cancellationToken);

        var personalTenantId = workspaceContext.PersonalTenantId;
        var activeTenantName = workspaceContext.ActiveTenantName;

        // Dispatch hybrid retrieval across module boundary via MediatR to query Documents candidate chunks.
        var retrievalResult = await mediator.Send(
            new Normora.Shared.Contracts.Documents.PerformHybridRetrievalQuery(
                rewrittenQuestion,
                clampedLimit,
                personalTenantId),
            cancellationToken);

        var topCandidates = retrievalResult.Candidates;
        var vectorSimOfFirst = retrievalResult.VectorSimOfFirst;

        var historyTurns = AskHelpers.BuildConversationTurns(budgetedHistory);

        var toolContext = new ToolExecutionContext(
            tenantContext.TenantId!.Value,
            activeTenantName,
            currentUser.ExternalUserId,
            currentUser.Email,
            currentUser.DisplayName,
            personalTenantId);

        return new ConversationTurnContext(
            conversation,
            userMessage,
            rewrittenQuestion,
            wasRewritten,
            topCandidates,
            vectorSimOfFirst,
            historyTurns,
            activeTenantName,
            toolContext,
            isFirstTurn);
    }
}

