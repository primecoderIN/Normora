namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Service responsible for asynchronously summarizing the user's opening prompt into a concise conversation title.
/// </summary>
public interface IAutoTitleService
{
    /// <summary>
    /// Enqueues background title generation for the conversation based on the initial question.
    /// </summary>
    void AutoTitleConversationAsync(Guid conversationId, string firstQuestion);
}
