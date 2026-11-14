using MediatR;
using Normora.Modules.Conversations.Application.Approvals.DTOs;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.DismissPendingApproval;

/// <summary>
/// Cancels and dismisses a pending autonomous agent action.
/// </summary>
public sealed record DismissPendingApprovalCommand(
    Guid ApprovalId,
    Guid TenantId,
    Guid UserId) : IRequest<DismissApprovalResultDto>;

