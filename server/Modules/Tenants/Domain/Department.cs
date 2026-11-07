namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Domain Entity representing an organizational department within a tenant workspace.
/// Role in Conversational RAG & ACL:
/// 1. Departmental Scoping: Internal documents can be restricted to specific departments.
/// 2. Access Resolution: Employees acquire departmental visibility through direct assignment
///    (MembershipDepartment) or inherited through user groups (UserGroupDepartment).
/// 3. Invariant: Department names must be unique within a tenant workspace (case-insensitive).
/// </summary>
public class Department : Normora.Shared.Interfaces.ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>The tenant workspace this department belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The display name of the department (e.g. "Engineering", "Legal", "People Ops").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>An optional description of the department's responsibilities.</summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<MembershipDepartment> MembershipDepartments { get; set; } = new List<MembershipDepartment>();
    public ICollection<UserGroupDepartment> UserGroupDepartments { get; set; } = new List<UserGroupDepartment>();
}
