namespace Normora.Shared.Interfaces;

/// <summary>
/// Result of an agent tool execution.
/// </summary>
/// <param name="Success">Indicates whether the tool completed without failure.</param>
/// <param name="Output">The tool's textual or serialized output presented back to the LLM or caller.</param>
/// <param name="Error">Optional error details if execution was unsuccessful.</param>
/// <param name="StructuredData">Optional typed data model returned by the tool for dynamic UI rendering.</param>
/// <param name="ChangedData">Indicates whether workspace state was modified (triggering reactive cache invalidation).</param>
public sealed record AgentToolResult(
    bool Success,
    string Output,
    string? Error = null,
    object? StructuredData = null,
    bool ChangedData = false);
