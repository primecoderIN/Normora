using MediatR;
using Normora.Shared.Contracts.Conversations;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Tools.Queries.GetMcpTools;

public sealed class GetMcpToolsQueryHandler(IAgentToolRegistry toolRegistry)
    : IRequestHandler<GetMcpToolsQuery, IReadOnlyList<McpToolDefinition>>
{
    public Task<IReadOnlyList<McpToolDefinition>> Handle(GetMcpToolsQuery request, CancellationToken cancellationToken)
    {
        var tools = toolRegistry.GetMcpToolDefinitions();
        return Task.FromResult(tools);
    }
}

