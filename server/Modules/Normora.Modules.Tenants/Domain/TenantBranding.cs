namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents the white-label branding configuration for a specific tenant.
/// Used to dynamically inject CSS variables into the frontend for a custom look and feel.
/// </summary>
public class TenantBranding
{
    /// <summary>
    /// Unique identifier for the branding record.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The associated Tenant ID this branding belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The primary brand color (e.g., hex code #3b82f6) used for main UI elements.
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// The secondary brand color used for accents.
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// URL to the tenant's main logo image.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// URL to the tenant's favicon.
    /// </summary>
    public string? FaviconUrl { get; set; }
    
    /// <summary>
    /// Timestamp of when the branding configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of when the branding configuration was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the owning Tenant.
    /// </summary>
    public Tenant Tenant { get; set; } = null!;
}
