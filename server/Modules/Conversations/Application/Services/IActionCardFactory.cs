using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Factory that transforms autonomous agent tool outputs into interactive A2UI action cards.
/// </summary>
public interface IActionCardFactory
{
    /// <summary>
    /// Builds an interactive action card for the specified tool result.
    /// </summary>
    /// <param name="toolName">The unique name of the executed tool.</param>
    /// <param name="argumentsJson">The raw JSON arguments passed to the tool.</param>
    /// <param name="result">The execution result returned by the tool.</param>
    /// <returns>An <see cref="ActionCardItem"/> if an applicable builder exists; otherwise, null.</returns>
    ActionCardItem? CreateCard(string toolName, string argumentsJson, AgentToolResult result);
}

/// <summary>
/// Defines a builder that constructs an A2UI action card for a specific agent tool.
/// </summary>
public interface IActionCardBuilder
{
    /// <summary>
    /// Determines whether this builder handles the specified tool.
    /// </summary>
    bool CanHandle(string toolName);

    /// <summary>
    /// Constructs the action card representation for the tool execution.
    /// </summary>
    ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result);
}

