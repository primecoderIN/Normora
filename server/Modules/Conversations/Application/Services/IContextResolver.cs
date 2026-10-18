namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Service interface for fetching past conversation messages within a sliding window.
/// </summary>
public interface IContextResolver
{
    /// <summary>
    /// Retrieves up to <paramref name="messageLimit"/> recent messages for prompt contextualization.
    /// </summary>
    Task<IReadOnlyList<ConversationMessageContext>> GetConversationContextAsync(
        Guid conversationId,
        string userId,
        int messageLimit = 5,
        CancellationToken cancellationToken = default);
}

