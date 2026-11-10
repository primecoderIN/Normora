using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Handles <see cref="AskConversationCommand"/> by delegating turn preparation (ownership, history, RAG)
/// to <see cref="IConversationTurnPreparer"/>, generating the conversational response, and saving the turn.
/// </summary>
internal sealed class AskConversationCommandHandler(
    IConversationTurnPreparer turnPreparer,
    ITextGenerationService generationService,
    ConversationsDbContext conversationsContext,
    ITenantContext tenantContext,
    ITokenBudgetService tokenBudget,
    IAutoTitleService autoTitleService,
    ConversationMetrics metrics) : IRequestHandler<AskConversationCommand, AskConversationResult>
{
    private const double MinimumSimilarity = 0.40;

    public async Task<AskConversationResult> Handle(
        AskConversationCommand request,
        CancellationToken cancellationToken)
    {
        if (!generationService.IsConfigured)
            throw new InvalidOperationException(ApiMessages.LlmGenerationNotConfigured);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 1-5. Validate, persist user message, prepare history & hybrid retrieval via turn preparer
            var turn = await turnPreparer.PrepareTurnAsync(
                request.ConversationId,
                request.Question,
                request.Limit,
                cancellationToken);

            // 6. Multi-turn grounded generation with Tool Calling
            string answerText;
            List<MessageCitation> citations = [];

            var topScore = turn.TopCandidates.Count > 0 ? turn.TopCandidates[0].Score : 0.0;
            var relevantCandidates = turn.TopCandidates
                .Where(c => c.Score >= MinimumSimilarity && (topScore == 0 || c.Score >= topScore * 0.70))
                .ToList();

            if (relevantCandidates.Count == 0 ||
                (turn.VectorSimOfFirst > 0 && turn.VectorSimOfFirst < MinimumSimilarity))
            {
                answerText = await generationService.GenerateConversationalAnswerAsync(
                    turn.RewrittenQuestion, [], turn.HistoryTurns, turn.ActiveTenantName, turn.ToolContext, cancellationToken);
            }
            else
            {
                var sources = relevantCandidates
                    .Select(c => new AskSource(c.FileName, c.ChunkIndex, c.Content))
                    .ToList();

                answerText = await generationService.GenerateConversationalAnswerAsync(
                    turn.RewrittenQuestion, sources, turn.HistoryTurns, turn.ActiveTenantName, turn.ToolContext, cancellationToken);

                // Group by document ID to deduplicate citations in the UI, selecting the highest-scoring chunk per document
                citations = relevantCandidates
                    .GroupBy(c => c.DocumentId)
                    .Select((g, idx) =>
                    {
                        var best = g.OrderByDescending(c => c.Score).First();
                        return new MessageCitation
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantContext.TenantId!.Value,
                            DocumentId = best.DocumentId,
                            DocumentChunkId = best.ChunkId,
                            FileName = best.FileName,
                            Rank = idx + 1,
                            Score = best.Score,
                            RetrievalMethod = RetrievalMethod.Hybrid
                        };
                    })
                    .ToList();
            }

            // 7. Persist assistant message + citations
            var assistantMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = request.ConversationId,
                TenantId = tenantContext.TenantId!.Value,
                Role = MessageRole.Assistant,
                Content = answerText,
                CreatedAt = DateTimeOffset.UtcNow,
                TokenCount = tokenBudget.EstimateTokenCount(answerText),
                RetrievalQuery = turn.WasRewritten ? turn.RewrittenQuestion : null,
                Rewritten = turn.WasRewritten,
                RequiresContext = true
            };

            foreach (var citation in citations)
            {
                citation.MessageId = assistantMessage.Id;
                assistantMessage.Citations.Add(citation);
            }

            conversationsContext.Messages.Add(assistantMessage);

            turn.Conversation.LastMessageAt = DateTimeOffset.UtcNow;
            turn.Conversation.UpdatedAt = DateTimeOffset.UtcNow;

            await conversationsContext.SaveChangesAsync(cancellationToken);

            // 8. Auto-title on first turn
            if (turn.IsFirstTurn && turn.Conversation.Title == ConversationConstants.DefaultTitle)
            {
                autoTitleService.AutoTitleConversationAsync(turn.Conversation.Id, request.Question);
            }

            metrics.TokensConsumed.Add((turn.UserMessage.TokenCount ?? 0) + (assistantMessage.TokenCount ?? 0), new KeyValuePair<string, object?>("operation", "ask"));

            return new AskConversationResult(
                request.ConversationId,
                turn.UserMessage.Id,
                assistantMessage.Id,
                answerText,
                citations.Select(c => new AskConversationCitation(
                    c.DocumentId,
                    c.FileName,
                    0,
                    c.Score,
                    c.DocumentChunkId)).ToList());
        }
        finally
        {
            stopwatch.Stop();
            metrics.RagDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("operation", "ask"));
        }
    }
}

