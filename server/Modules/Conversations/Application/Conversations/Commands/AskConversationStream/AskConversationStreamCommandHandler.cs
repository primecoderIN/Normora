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
/// Stream handler for <see cref="AskConversationStreamCommand"/>, delegating turn preparation
/// to <see cref="IConversationTurnPreparer"/>, streaming generated LLM tokens via SSE events,
/// and persisting the assistant message once streaming completes.
/// </summary>
internal sealed class AskConversationStreamCommandHandler(
    IConversationTurnPreparer turnPreparer,
    ConversationsDbContext conversationsContext,
    ITextGenerationService generationService,
    IAutoTitleService autoTitleService,
    ITenantContext tenantContext,
    ITokenBudgetService tokenBudget,
    ConversationMetrics metrics) : IStreamRequestHandler<AskConversationStreamCommand, AskConversationStreamEvent>
{
    private const double MinimumSimilarity = 0.40;

    public async IAsyncEnumerable<AskConversationStreamEvent> Handle(
        AskConversationStreamCommand request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Stream Request Validation Guard:
        // Because MediatR stream requests (IStreamRequest<T>) bypass IPipelineBehavior<TRequest, TResponse>,
        // we validate bounds directly here to protect downstream LLM and database token budgets.
        if (request.ConversationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 4000)
        {
            throw new ArgumentException(ApiMessages.ValidationFailed);
        }

        // Provider Verification:
        // Ensures Gemini API text generation service is configured before accepting streaming requests.
        if (!generationService.IsConfigured)
            throw new InvalidOperationException(ApiMessages.LlmGenerationNotConfigured);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Pipeline Step 1-5: Turn Preparation (IConversationTurnPreparer)
            // Executes conversation validation, BOLA ownership verification, user message persistence,
            // token-budgeted history assembly, LLM query rewriting, and cross-module hybrid retrieval (BM25 + pgvector).
            var turn = await turnPreparer.PrepareTurnAsync(
                request.ConversationId,
                request.Question,
                request.Limit,
                cancellationToken);

            // Pipeline Step 6: Early Citation Emission
            // Citations are emitted to the client SSE stream immediately before LLM generation begins.
            // Citations and LLM context sources are strictly filtered to relevant chunks:
            // 1. Chunk score must meet absolute MinimumSimilarity (0.40).
            // 2. Chunk score must not drop below 70% of the top match (rejects unrelated background noise).
            var assistantMessageId = Guid.NewGuid();
            var citations = new List<MessageCitation>();
            var mappedSources = new List<AskConversationCitation>();

            var topScore = turn.TopCandidates.Count > 0 ? turn.TopCandidates[0].Score : 0.0;
            var relevantCandidates = turn.TopCandidates
                .Where(c => c.Score >= MinimumSimilarity && (topScore == 0 || c.Score >= topScore * 0.70))
                .ToList();

            if (relevantCandidates.Count > 0 && (turn.VectorSimOfFirst == 0 || turn.VectorSimOfFirst >= MinimumSimilarity))
            {
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

                mappedSources = citations.Select(c => new AskConversationCitation(
                    c.DocumentId,
                    c.FileName,
                    0,
                    c.Score,
                    c.DocumentChunkId)).ToList();
            }

            // Yield CitationsReadyEvent event across SSE stream
            yield return new CitationsReadyEvent(turn.UserMessage.Id, assistantMessageId, mappedSources);

            // Pipeline Step 7: Streaming LLM Answer Generation
            // Grounded RAG with Hallucination Defense:
            // If no relevant candidates meet the threshold, return deterministic fallback message
            var answerBuilder = new System.Text.StringBuilder();

            if (relevantCandidates.Count == 0 || (turn.VectorSimOfFirst > 0 && turn.VectorSimOfFirst < MinimumSimilarity))
            {
                var text = RagConstants.FallbackAnswer;
                answerBuilder.Append(text);
                yield return new TextChunkEvent(text);
            }
            else
            {
                var sources = relevantCandidates
                    .Select(c => new AskSource(c.FileName, c.ChunkIndex, c.Content))
                    .ToList();

                // Stream token chunks as they arrive from Gemini API (with agent tool support)
                await foreach (var chunk in generationService.StreamConversationalAnswerAsync(
                    turn.RewrittenQuestion, sources, turn.HistoryTurns, turn.ActiveTenantName, turn.ToolContext, cancellationToken))
                {
                    answerBuilder.Append(chunk);
                    yield return new TextChunkEvent(chunk);
                }
            }

            var answerText = answerBuilder.ToString();

            // Pipeline Step 8: Assistant Message Persistence & Audit Linkage
            // Save the complete generated answer along with token usage and citation foreign keys into the database.
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

            // Pipeline Step 9: Asynchronous Fire-and-Forget Auto-Titling
            // On the first conversational exchange, summarize the initial user query into a concise 3-5 word title.
            if (turn.IsFirstTurn && turn.Conversation.Title == ConversationConstants.DefaultTitle)
            {
                autoTitleService.AutoTitleConversationAsync(turn.Conversation.Id, request.Question);
            }

            // OpenTelemetry Metrics: Record token consumption and turn completion
            metrics.TokensConsumed.Add((turn.UserMessage.TokenCount ?? 0) + (assistantMessage.TokenCount ?? 0), new KeyValuePair<string, object?>("operation", "ask"));

            // Signal stream completion to the frontend event listener
            yield return new StreamFinishedEvent();
        }
        finally
        {
            stopwatch.Stop();
            metrics.RagDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("operation", "ask"));
        }
    }
}

