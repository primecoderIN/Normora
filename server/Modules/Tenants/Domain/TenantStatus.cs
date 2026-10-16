namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents the operational status of a tenant workspace.
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// The tenant is active and fully functional.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The tenant has been suspended (e.g., due to billing issues). Access is blocked for all members.
    /// </summary>
    Suspended = 2
}
