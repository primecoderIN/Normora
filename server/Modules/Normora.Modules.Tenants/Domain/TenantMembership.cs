namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// A many-to-many junction entity linking a User to a Tenant.
/// It also defines the specific Role (Employer, Employee) that user holds within the context of that tenant.
/// </summary>
public class TenantMembership
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The tenant the user is a member of.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The user who has been granted access.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The RBAC role assigned to this user for this specific tenant (e.g. Employer vs Employee).
    /// </summary>
    public TenantRole Role { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;

    /// <summary>Departments directly assigned to this membership.</summary>
    public ICollection<MembershipDepartment> MembershipDepartments { get; set; } = new List<MembershipDepartment>();

    /// <summary>User groups this membership belongs to.</summary>
    public ICollection<UserGroupMembership> UserGroupMemberships { get; set; } = new List<UserGroupMembership>();
}
