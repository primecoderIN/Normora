using MediatR;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.CreatePendingApproval;

/// <summary>
/// Persists a human-in-the-loop approval request for an autonomous agent action.
/// </summary>
public sealed record CreatePendingApprovalCommand(
    Guid TenantId,
    Guid UserId,
    Guid? ConversationId,
    string ToolName,
    string ArgumentsJson,
    string? SnapshotJson = null,
    TimeSpan? Ttl = null) : IRequest<Guid>;

