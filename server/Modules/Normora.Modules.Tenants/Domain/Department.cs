namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents an organizational department or team within a tenant (e.g., "Frontend", "HR").
/// Documents can be scoped to one or more departments. Users access departmental documents
/// via direct assignment or through User Group inheritance.
/// </summary>
public class Department
{
    public Guid Id { get; set; }

    /// <summary>The tenant this department belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The display name of the department (e.g., "Frontend", "HR").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>An optional description of the department's purpose.</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<MembershipDepartment> MembershipDepartments { get; set; } = new List<MembershipDepartment>();
    public ICollection<UserGroupDepartment> UserGroupDepartments { get; set; } = new List<UserGroupDepartment>();
}
