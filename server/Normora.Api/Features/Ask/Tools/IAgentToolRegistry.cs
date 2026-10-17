namespace Normora.Api.Features.Ask.Tools;

/// <summary>
/// Registry containing all registered AI agent tools.
/// Handles translation to Gemini Function Declaration formats and MCP Tool Schemas.
/// </summary>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Gets all registered agent tools.
    /// </summary>
    IReadOnlyList<IAgentTool> Tools { get; }

    /// <summary>
    /// Finds a registered tool by its unique name.
    /// </summary>
    IAgentTool? FindTool(string name);

    /// <summary>
    /// Returns the Gemini-compatible tools declaration object to attach to Gemini generateContent requests.
    /// </summary>
    object GetGeminiToolsDeclaration();

    /// <summary>
    /// Executes the specified tool safely by name.
    /// </summary>
    Task<AgentToolResult> ExecuteToolAsync(
        string name,
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the tools list formatted for Model Context Protocol (MCP) tools/list responses.
    /// </summary>
    IReadOnlyList<McpToolDefinition> GetMcpToolDefinitions();
}

/// <summary>
/// Tool definition structure conforming to the Model Context Protocol (MCP) JSON-RPC specification.
/// </summary>
public sealed record McpToolDefinition(
    string Name,
    string Description,
    object InputSchema);
