namespace Normora.Api.Features.Ask.Tools;

/// <summary>
/// Default implementation of <see cref="IAgentToolRegistry"/> that aggregates all registered tools.
/// </summary>
public sealed class AgentToolRegistry(IEnumerable<IAgentTool> tools) : IAgentToolRegistry
{
    private readonly List<IAgentTool> _toolsList = tools.ToList();

    public IReadOnlyList<IAgentTool> Tools => _toolsList;

    public IAgentTool? FindTool(string name) =>
        _toolsList.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    public object GetGeminiToolsDeclaration()
    {
        return new
        {
            functionDeclarations = _toolsList.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.ParameterSchema
            }).ToList()
        };
    }

    public async Task<AgentToolResult> ExecuteToolAsync(
        string name,
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var tool = FindTool(name);
        if (tool == null)
        {
            return new AgentToolResult(false, string.Empty, $"Tool '{name}' was not found in the workspace agent registry.");
        }

        return await tool.ExecuteAsync(argumentsJson, context, cancellationToken);
    }

    public IReadOnlyList<McpToolDefinition> GetMcpToolDefinitions() =>
        _toolsList.Select(t => new McpToolDefinition(t.Name, t.Description, t.ParameterSchema)).ToList();
}
