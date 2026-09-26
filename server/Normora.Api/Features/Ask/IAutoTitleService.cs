namespace Normora.Api.Features.Ask;

public interface IAutoTitleService
{
    void AutoTitleConversationAsync(Guid conversationId, string firstQuestion);
}
