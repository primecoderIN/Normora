using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Shared.Contracts.Conversations;

/// <summary>
/// Cross-module query to retrieve registered agent tools formatted for MCP without leaking Conversations internals.
/// </summary>
public record GetMcpToolsQuery : IRequest<IReadOnlyList<McpToolDefinition>>;

