namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Domain Entity representing a collective RBAC group within a tenant workspace.
/// Architecture & Permissions:
/// 1. Group Inheritance: Groups map collections of employees (UserGroupMembership) to multiple
///    departments (UserGroupDepartment), eliminating the need for manual single-department assignments.
/// 2. SEC-17 Boundary: When assigning departments to a group, cross-tenant department IDs are strictly rejected.
/// 3. Invariant: Group names must be unique within a tenant workspace (case-insensitive).
/// </summary>
public class UserGroup : Normora.Shared.Interfaces.ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>The tenant workspace this group belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The display name of the group (e.g. "Core Engineering", "Leadership").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>An optional description of the group's purpose.</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<UserGroupMembership> UserGroupMemberships { get; set; } = new List<UserGroupMembership>();
    public ICollection<UserGroupDepartment> UserGroupDepartments { get; set; } = new List<UserGroupDepartment>();
}
