namespace Normora.Shared.Interfaces;

/// <summary>
/// Result of an agent tool execution.
/// </summary>
/// <param name="Success">Indicates whether the tool completed without failure.</param>
/// <param name="Output">The tool's textual or serialized output presented back to the LLM or caller.</param>
/// <param name="Error">Optional error details if execution was unsuccessful.</param>
public sealed record AgentToolResult(
    bool Success,
    string Output,
    string? Error = null);
