namespace Normora.Modules.Conversations.Application.Services;

public record ConversationMessageContext(
    string Role,
    string Content);

public interface IQueryRewriterService
{
    Task<string> RewriteQueryAsync(
        string currentQuestion,
        IReadOnlyList<ConversationMessageContext> conversationHistory,
        CancellationToken cancellationToken = default);
}
