using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Prepared RAG turn context containing conversation entity, user message, rewritten query,
/// hybrid retrieval candidates, conversation history turns, and tool execution context.
/// </summary>
public sealed record ConversationTurnContext(
    Conversation Conversation,
    Message UserMessage,
    string RewrittenQuestion,
    bool WasRewritten,
    IReadOnlyList<RetrievalCandidate> TopCandidates,
    double VectorSimOfFirst,
    IReadOnlyList<ConversationTurn> HistoryTurns,
    string ActiveTenantName,
    ToolExecutionContext ToolContext,
    bool IsFirstTurn);

/// <summary>
/// Prepares a conversational turn by validating ownership, persisting the user message,
/// trimming conversation history, rewriting the query, and executing hybrid document retrieval.
/// </summary>
public interface IConversationTurnPreparer
{
    Task<ConversationTurnContext> PrepareTurnAsync(
        Guid conversationId,
        string question,
        int limit,
        CancellationToken cancellationToken);
}

