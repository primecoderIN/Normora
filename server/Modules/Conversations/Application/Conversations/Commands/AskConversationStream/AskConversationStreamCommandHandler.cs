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
    private const double MinimumSimilarity = 0.35;

    public async IAsyncEnumerable<AskConversationStreamEvent> Handle(
        AskConversationStreamCommand request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
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
            // This allows the frontend to render document badges and source tabs instantly while text streams.
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

            // Yield CitationsReadyEvent event across SSE stream
            yield return new CitationsReadyEvent(turn.UserMessage.Id, assistantMessageId, mappedSources);

            // Pipeline Step 7: Streaming LLM Answer Generation
            // Grounded RAG with Hallucination Defense:
            // If top candidate similarity is below MinimumSimilarity threshold (0.35), we return a deterministic
            // fallback message ("I could not find relevant information in company documents") to prevent hallucinations.
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

