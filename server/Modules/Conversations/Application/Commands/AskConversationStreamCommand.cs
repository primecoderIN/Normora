using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Commands;

/// <summary>
/// Streaming variant of <see cref="AskConversationCommand"/>. Same RAG pipeline but yields
/// <see cref="AskConversationStreamEvent"/> instances via SSE instead of returning a single result.
/// </summary>
public sealed record AskConversationStreamCommand(
    Guid ConversationId,
    string Question,
    int Limit = 5) : IStreamRequest<AskConversationStreamEvent>;

public sealed class AskConversationStreamCommandHandler(
    IConversationTurnPreparer turnPreparer,
    ConversationsDbContext conversationsContext,
    ITextGenerationService generationService,
    IAutoTitleService autoTitleService,
    ITenantContext tenantContext,
    ITokenBudgetService tokenBudget,
    ConversationMetrics metrics) : IStreamRequestHandler<AskConversationStreamCommand, AskConversationStreamEvent>
{
    private const double MinimumSimilarity = 0.35;

    public async IAsyncEnumerable<AskConversationStreamEvent> Handle(
        AskConversationStreamCommand request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
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

            // 6. Build citations + emit immediately
            var assistantMessageId = Guid.NewGuid();
            var citations = new List<MessageCitation>();
            var mappedSources = new List<AskConversationCitation>();

            if (turn.TopCandidates.Count > 0 && (turn.VectorSimOfFirst == 0 || turn.VectorSimOfFirst >= MinimumSimilarity))
            {
                citations = turn.TopCandidates.Select((c, idx) => new MessageCitation
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

                mappedSources = turn.TopCandidates.Select(c => new AskConversationCitation(
                    c.DocumentId,
                    c.FileName,
                    c.ChunkIndex,
                    c.Score)).ToList();
            }

            // Emit citations before streaming text
            yield return new CitationsReadyEvent(turn.UserMessage.Id, assistantMessageId, mappedSources);

            // 7. Stream generation
            var answerBuilder = new System.Text.StringBuilder();

            if (turn.TopCandidates.Count == 0 || (turn.VectorSimOfFirst > 0 && turn.VectorSimOfFirst < MinimumSimilarity))
            {
                var text = RagConstants.FallbackAnswer;
                answerBuilder.Append(text);
                yield return new TextChunkEvent(text);
            }
            else
            {
                var sources = turn.TopCandidates
                    .Select(c => new AskSource(c.FileName, c.ChunkIndex, c.Content))
                    .ToList();

                await foreach (var chunk in generationService.StreamConversationalAnswerAsync(
                    turn.RewrittenQuestion, sources, turn.HistoryTurns, turn.ActiveTenantName, cancellationToken))
                {
                    answerBuilder.Append(chunk);
                    yield return new TextChunkEvent(chunk);
                }
            }

            var answerText = answerBuilder.ToString();

            // 8. Persist assistant message + citations
            var assistantMessage = new Message
            {
                Id = assistantMessageId,
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

            // 9. Auto-title on first turn
            if (turn.IsFirstTurn && turn.Conversation.Title == ConversationConstants.DefaultTitle)
            {
                autoTitleService.AutoTitleConversationAsync(turn.Conversation.Id, request.Question);
            }

            metrics.TokensConsumed.Add((turn.UserMessage.TokenCount ?? 0) + (assistantMessage.TokenCount ?? 0), new KeyValuePair<string, object?>("operation", "ask"));

            yield return new StreamFinishedEvent();
        }
        finally
        {
            stopwatch.Stop();
            metrics.RagDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("operation", "ask"));
        }
    }
}
