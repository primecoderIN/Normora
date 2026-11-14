using MediatR;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.CreatePendingApproval;

/// <summary>
/// Command handler that creates a new pending approval record.
/// </summary>
public sealed class CreatePendingApprovalCommandHandler(ConversationsDbContext context)
    : IRequestHandler<CreatePendingApprovalCommand, Guid>
{
    public async Task<Guid> Handle(CreatePendingApprovalCommand request, CancellationToken cancellationToken)
    {
        var ttl = request.Ttl ?? TimeSpan.FromHours(24);
        var approval = new PendingApproval
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ToolName = request.ToolName,
            ArgumentsJson = request.ArgumentsJson,
            SnapshotJson = request.SnapshotJson,
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl)
        };

        context.PendingApprovals.Add(approval);
        await context.SaveChangesAsync(cancellationToken);

        return approval.Id;
    }
}

