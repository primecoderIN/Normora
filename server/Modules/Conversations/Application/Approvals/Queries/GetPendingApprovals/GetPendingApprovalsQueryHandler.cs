using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Approvals.DTOs;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;

namespace Normora.Modules.Conversations.Application.Approvals.Queries.GetPendingApprovals;

/// <summary>
/// Query handler returning pending approvals for the current user in the active tenant.
/// </summary>
public sealed class GetPendingApprovalsQueryHandler(ConversationsDbContext context)
    : IRequestHandler<GetPendingApprovalsQuery, IReadOnlyList<PendingApprovalDto>>
{
    public async Task<IReadOnlyList<PendingApprovalDto>> Handle(GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = context.PendingApprovals
            .AsNoTracking()
            .Where(p => p.TenantId == request.TenantId &&
                        p.UserId == request.UserId &&
                        p.Status == ApprovalStatus.Pending &&
                        p.ExpiresAt > now);

        if (request.ConversationId.HasValue)
        {
            query = query.Where(p => p.ConversationId == request.ConversationId.Value);
        }

        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PendingApprovalDto(
                p.Id,
                p.ConversationId,
                p.UserId,
                p.ToolName,
                p.ArgumentsJson,
                p.SnapshotJson,
                p.Status.ToString(),
                p.CreatedAt,
                p.ExpiresAt,
                p.ExecutedAt))
            .ToListAsync(cancellationToken);

        return list;
    }
}

