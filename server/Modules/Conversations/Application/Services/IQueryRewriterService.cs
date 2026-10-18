namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Simplified context message entry representing a historical conversation turn.
/// </summary>
/// <param name="Role">Sender role (User or Assistant).</param>
/// <param name="Content">Text content of the message.</param>
public record ConversationMessageContext(
    string Role,
    string Content);

/// <summary>
/// Rewrites conversational questions into standalone retrieval queries by resolving coreferences.
/// </summary>
public interface IQueryRewriterService
{
    /// <summary>
    /// Transforms the user's current query into a self-contained question using recent chat history.
    /// </summary>
    Task<string> RewriteQueryAsync(
        string currentQuestion,
        IReadOnlyList<ConversationMessageContext> conversationHistory,
        CancellationToken cancellationToken = default);
}

