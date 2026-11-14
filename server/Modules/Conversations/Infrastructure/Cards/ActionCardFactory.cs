using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Composite factory that iterates registered <see cref="IActionCardBuilder"/> instances to construct dynamic action cards.
/// </summary>
public sealed class ActionCardFactory(IEnumerable<IActionCardBuilder> builders) : IActionCardFactory
{
    private readonly List<IActionCardBuilder> _buildersList = builders.ToList();

    public ActionCardItem? CreateCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        var builder = _buildersList.FirstOrDefault(b => b.CanHandle(toolName));
        return builder?.BuildCard(toolName, argumentsJson, result);
    }
}

