using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Approvals.DTOs;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.DismissPendingApproval;

/// <summary>
/// Handler that dismisses a pending approval.
/// </summary>
public sealed class DismissPendingApprovalCommandHandler(ConversationsDbContext context)
    : IRequestHandler<DismissPendingApprovalCommand, DismissApprovalResultDto>
{
    public async Task<DismissApprovalResultDto> Handle(DismissPendingApprovalCommand request, CancellationToken cancellationToken)
    {
        var approval = await context.PendingApprovals
            .FirstOrDefaultAsync(p => p.Id == request.ApprovalId && p.TenantId == request.TenantId, cancellationToken);

        if (approval == null)
        {
            throw new BolaException();
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            return new DismissApprovalResultDto(false, "This action request has already been decided.");
        }

        approval.Status = ApprovalStatus.Dismissed;
        await context.SaveChangesAsync(cancellationToken);

        return new DismissApprovalResultDto(true, "Action was successfully dismissed.");
    }
}

