namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Many-to-many junction entity associating an internal User with a Tenant workspace.
/// Carries role-based access control (Admin / Employee), direct departmental mappings,
/// user group memberships, and soft-delete audit tracking.
/// </summary>
public class TenantMembership : Normora.Shared.Interfaces.ITenantEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign key identifier of the tenant workspace.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Foreign key identifier of the internal user profile.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Workspace RBAC Role: TenantRole.Admin (full administrative management) vs TenantRole.Employee.
    /// </summary>
    public TenantRole Role { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;

    /// <summary>Directly assigned departments granting explicit knowledge retrieval access.</summary>
    public ICollection<MembershipDepartment> MembershipDepartments { get; set; } = new List<MembershipDepartment>();

    /// <summary>User groups granting aggregate departmental permissions.</summary>
    public ICollection<UserGroupMembership> UserGroupMemberships { get; set; } = new List<UserGroupMembership>();

    /// <summary>
    /// Soft-Delete Audit Timestamp:
    /// When an employee is removed, RemovedAt is stamped while preserving audit trail entries.
    /// Filtered automatically by EF Core global multitenant query filters ("RemovedAt IS NULL").
    /// </summary>
    public DateTime? RemovedAt { get; set; }

    /// <summary>
    /// The UserId of the administrator who executed the member removal.
    /// </summary>
    public Guid? RemovedByUserId { get; set; }
}
