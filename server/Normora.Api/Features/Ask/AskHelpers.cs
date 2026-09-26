using Normora.Shared.Constants;
using Normora.Modules.Conversations.Domain;

namespace Normora.Api.Features.Ask;

public static class AskHelpers
{
    /// <summary>
    /// Converts a flat chronological list of messages into paired (User, Assistant) turns
    /// for injection into the generation prompt. Unpaired trailing messages are discarded.
    /// </summary>
    public static List<ConversationTurn> BuildConversationTurns(
        IReadOnlyList<Normora.Modules.Conversations.Application.Services.ConversationMessageContext> history)
    {
        var turns = new List<ConversationTurn>();
        for (int i = 0; i + 1 < history.Count; i += 2)
        {
            var a = history[i];
            var b = history[i + 1];
            // Ensure the pair is user → assistant (guard against unexpected ordering)
            if (string.Equals(a.Role, MessageRole.User.ToString(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Role, MessageRole.Assistant.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                turns.Add(new ConversationTurn(a.Content, b.Content));
            }
        }
        return turns;
    }
}
