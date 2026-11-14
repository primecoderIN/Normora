using MediatR;
using Normora.Modules.Conversations.Application.Approvals.Commands.CreatePendingApproval;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Services;

/// <summary>
/// Infrastructure service coordinating human-in-the-loop (HITL) approvals via MediatR commands.
/// </summary>
public sealed class AgentApprovalService(IMediator mediator) : IAgentApprovalService
{
    public Task<Guid> RequestApprovalAsync(
        Guid tenantId,
        string userId,
        Guid? conversationId,
        string toolName,
        string argumentsJson,
        string? snapshotJson = null,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var parsedUserId = Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;

        return mediator.Send(
            new CreatePendingApprovalCommand(
                tenantId,
                parsedUserId,
                conversationId,
                toolName,
                argumentsJson,
                snapshotJson,
                ttl),
            cancellationToken);
    }
}

