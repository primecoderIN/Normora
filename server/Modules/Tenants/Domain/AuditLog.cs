namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents an immutable audit trail entry recording sensitive tenant mutations.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? Metadata { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
