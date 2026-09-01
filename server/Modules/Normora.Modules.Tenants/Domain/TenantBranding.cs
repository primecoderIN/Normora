namespace Normora.Modules.Tenants.Domain;

public class TenantBranding
{
    public Guid Id { get; set; }
    
    public Guid TenantId { get; set; }

    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Property
    public Tenant Tenant { get; set; } = null!;
}
