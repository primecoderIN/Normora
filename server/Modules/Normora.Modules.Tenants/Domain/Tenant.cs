namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents a discrete organization or workspace within the system.
/// In a multi-tenant architecture, all data across modules is partitioned by the Tenant ID.
/// </summary>
public class Tenant
{
    /// <summary>
    /// The unique identifier of the tenant.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The human-readable name of the tenant (e.g., "Acme Corp").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A URL-friendly version of the tenant name, often used in routing or subdomains (e.g., "acme-corp").
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// The current lifecycle status of the tenant (Active, Suspended, etc.).
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    /// <summary>
    /// All users who have been granted access to this tenant.
    /// </summary>
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();

    /// <summary>
    /// The white-label branding configuration for this tenant (colors, logo, favicon).
    /// </summary>
    public TenantBranding? Branding { get; set; }
}
