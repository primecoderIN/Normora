namespace Normora.Modules.Conversations.Application.Services;

public interface IContextResolver
{
    Task<IReadOnlyList<ConversationMessageContext>> GetConversationContextAsync(
        Guid conversationId,
        string userId,
        int messageLimit = 5,
        CancellationToken cancellationToken = default);
}
