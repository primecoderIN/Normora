namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// A named group of users within a tenant. A User Group can be assigned to one or more
/// Departments, and any user in that group inherits access to those departments.
/// Example: "Engineering" group → Frontend, Backend, DevOps departments.
/// </summary>
public class UserGroup
{
    public Guid Id { get; set; }

    /// <summary>The tenant this group belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The display name of the group (e.g., "Engineering", "HR Team").</summary>
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
