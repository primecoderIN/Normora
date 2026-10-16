namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Junction entity assigning a TenantMembership (user in a tenant) to a UserGroup.
/// A user can belong to multiple groups within the same tenant.
/// </summary>
public class UserGroupMembership
{
    public Guid TenantMembershipId { get; set; }
    public Guid UserGroupId { get; set; }

    // Navigation Properties
    public TenantMembership TenantMembership { get; set; } = null!;
    public UserGroup UserGroup { get; set; } = null!;
}
