namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Junction entity that directly assigns a TenantMembership (user within a tenant)
/// to a specific Department. A user may be directly assigned to multiple departments.
/// </summary>
public class MembershipDepartment
{
    public Guid TenantMembershipId { get; set; }
    public Guid DepartmentId { get; set; }

    // Navigation Properties
    public TenantMembership TenantMembership { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
