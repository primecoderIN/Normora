using MediatR;
using Normora.Shared.Contracts.Conversations;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Tools.Commands.ExecuteMcpTool;

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

