namespace Normora.Shared.Interfaces;

/// <summary>
/// Contract for recording audit log entries within the tenant's tamper-evident trail.
/// </summary>
public interface IAuditService
{
    Task RecordAsync(
        Guid tenantId,
        string? actorUserId,
        string? actorEmail,
        string? actorName,
        string action,
        string? targetType,
        string? targetId,
        string? metadata,
        CancellationToken cancellationToken = default);
}
