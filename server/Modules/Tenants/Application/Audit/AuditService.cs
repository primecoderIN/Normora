using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Audit;

/// <summary>
/// Service implementation responsible for persisting immutable audit logs.
/// </summary>
public class AuditService(TenantsDbContext dbContext) : IAuditService
{
    public async Task RecordAsync(
        Guid tenantId,
        string? actorUserId,
        string? actorEmail,
        string? actorName,
        string action,
        string? targetType,
        string? targetId,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            ActorName = actorName,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Metadata = metadata,
            OccurredAt = DateTime.UtcNow
        };

        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
