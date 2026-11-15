namespace Normora.Shared.Interfaces;

/// <summary>
/// Defines an executable agent tool that can be invoked via Gemini Function Calling
/// or exposed externally via the Model Context Protocol (MCP).
/// </summary>
public interface IAgentTool
{
    /// <summary>
    /// Unique identifier for the function (e.g. "calculate_policy_limit").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human- and LLM-readable description explaining when and how the tool should be called.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// JSON schema describing the function's parameters, compatible with OpenAPI / Gemini FunctionDeclaration.
    /// May be null or omitted if the function accepts no parameters.
    /// </summary>
    object? ParameterSchema => null;

    /// <summary>
    /// Operational category indicating whether the tool is read-only, mutating, or destructive.
    /// </summary>
    ToolCategory Category => ToolCategory.ReadOnly;

    /// <summary>
    /// Indicates whether human-in-the-loop confirmation is required before execution.
    /// </summary>
    bool RequiresApproval => Category == ToolCategory.Destructive;

    /// <summary>
    /// Optional JSON schema describing the tool's structured output.
    /// </summary>
    object? OutputSchema => null;

    /// <summary>
    /// Human-friendly localized labels for real-time streaming progress chips.
    /// </summary>
    ToolChipDescriptor? ChipDescriptor => null;

    /// <summary>
    /// Executes the tool with the given JSON arguments string in the context of the calling user and workspace.
    /// </summary>
    /// <param name="argumentsJson">Raw JSON arguments payload passed by the caller or model.</param>
    /// <param name="context">Tenant and user contextual metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing formatted textual response and optional structured data.</returns>
    Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default);
}
