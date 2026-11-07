namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Root Aggregate Entity for Multi-Tenancy.
/// Represents a discrete organizational workspace or individual workspace within Normora.
/// Enforces data isolation boundaries: all documents, embeddings, conversations, departments,
/// user groups, and subscription quotas are partitioned by TenantId.
/// </summary>
public class Tenant
{
    /// <summary>
    /// The unique identifier of the tenant workspace.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The human-readable name of the workspace (e.g. "Acme Corp").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe slug used for frontend route paths (/app/workspaces/{slug}/...) and optional subdomains.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// The operational lifecycle status of the tenant workspace (Active, Suspended).
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    /// <summary>
    /// Distinguishes auto-provisioned personal user workspaces (IsPersonal = true)
    /// from collaborative organizational workspaces.
    /// </summary>
    public bool IsPersonal { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public void Suspend()
    {
        // Domain Transition:
        // Transitioning to Suspended immediately disables access in TenantResolutionMiddleware.
        Status = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        // Domain Transition:
        // Re-enables access for all active tenant members.
        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    // Navigation Properties
    /// <summary>
    /// All users who have been granted access to this tenant.
    /// </summary>
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();

    /// <summary>
    /// The white-label branding configuration for this tenant (colors, logo, favicon).
    /// </summary>
    public TenantBranding? Branding { get; set; }

    /// <summary>
    /// Departments defined within this tenant (e.g., "Frontend", "HR").
    /// </summary>
    public ICollection<Department> Departments { get; set; } = new List<Department>();

    /// <summary>
    /// Named groups of users for access control within this tenant.
    /// </summary>
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}
