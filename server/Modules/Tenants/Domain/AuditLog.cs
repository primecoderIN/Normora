namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Domain Entity representing an immutable audit log entry.
/// Intercepted automatically by AuditBehavior<,> when commands implementing IAuditableRequest complete.
/// Invariants:
/// - Immutable: Once written, audit entries cannot be modified or updated.
/// - Scoped by TenantId: Administrators can query historical audit logs exclusively for their active tenant.
/// - Stores actor details (ExternalUserId, Email, DisplayName), action name, target entity, and JSON delta metadata.
/// </summary>
public class AuditLog : Normora.Shared.Interfaces.ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>External OIDC subject identifier of the actor who performed the action.</summary>
    public string? ActorUserId { get; set; }

    /// <summary>Verified email address of the actor.</summary>
    public string? ActorEmail { get; set; }

    /// <summary>Display name of the actor.</summary>
    public string? ActorName { get; set; }

    /// <summary>Action verb identifying the administrative event (e.g. "Department.Created", "Member.Removed").</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Entity type affected by the mutation (e.g. "Department", "UserGroup", "TenantBranding").</summary>
    public string? TargetType { get; set; }

    /// <summary>GUID or string identifier of the target resource.</summary>
    public string? TargetId { get; set; }

    /// <summary>Serialized JSON payload detailing previous vs updated property values.</summary>
    public string? Metadata { get; set; }

    /// <summary>Immutable UTC timestamp when the mutation completed.</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
