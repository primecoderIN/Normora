using MediatR;
using Normora.Modules.Conversations.Application.Approvals.DTOs;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.ConfirmPendingApproval;

/// <summary>
/// Authorizes and executes a pending autonomous agent action.
/// </summary>
public sealed record ConfirmPendingApprovalCommand(
    Guid ApprovalId,
    Guid TenantId,
    Guid UserId) : IRequest<ConfirmApprovalResultDto>;

