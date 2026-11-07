using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Shared.Contracts.Conversations;

/// <summary>
/// Cross-module command to execute an agent tool by name via MCP without leaking Conversations internals.
/// </summary>
public record ExecuteMcpToolCommand(
    string ToolName,
    string ArgumentsJson,
    ToolExecutionContext Context) : IRequest<AgentToolResult>;

