using MediatR;
using Normora.Modules.Conversations.Application.Approvals.DTOs;

namespace Normora.Modules.Conversations.Application.Approvals.Queries.GetPendingApprovals;

/// <summary>
/// Retrieves all active pending approval requests for the workspace user.
/// </summary>
public sealed record GetPendingApprovalsQuery(
    Guid TenantId,
    Guid UserId,
    Guid? ConversationId = null) : IRequest<IReadOnlyList<PendingApprovalDto>>;

