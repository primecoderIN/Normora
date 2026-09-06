namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents the authorization level a user possesses within a specific tenant context.
/// </summary>
public enum TenantRole
{
    /// <summary>
    /// Administrative privileges within the tenant (Employer).
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Standard employee privileges within the tenant.
    /// </summary>
    Employee = 2
}
