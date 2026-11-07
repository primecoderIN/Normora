using MediatR;
using Normora.Shared.Contracts.Conversations;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Tools.Commands.ExecuteMcpTool;

/// <summary>
/// Cross-module command handler that dispatches Model Context Protocol (MCP) tool execution requests
/// to the internal <see cref="IAgentToolRegistry"/> inside the Conversations module.
/// </summary>
/// <remarks>
/// Encapsulation:
/// - Allows the Documents McpController to execute conversational tools (e.g. search_company_documents)
///   via MediatR contracts without taking a project dependency on the Conversations module.
/// - Validates tool parameter JSON schema and returns standardized <see cref="AgentToolResult"/> payloads.
/// </remarks>
public sealed class ExecuteMcpToolCommandHandler(IAgentToolRegistry toolRegistry)
    : IRequestHandler<ExecuteMcpToolCommand, AgentToolResult>
{
    public async Task<AgentToolResult> Handle(ExecuteMcpToolCommand request, CancellationToken cancellationToken)
    {
        return await toolRegistry.ExecuteToolAsync(
            request.ToolName,
            request.ArgumentsJson,
            request.Context,
            cancellationToken);
    }
}

