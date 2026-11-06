namespace Normora.Modules.Tenants.Application.Branding.DTOs;

/// <summary>
/// Data transfer object containing the branding details for a tenant.
/// </summary>
public record TenantBrandingDto(
    Guid TenantId,
    string TenantName,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    string? LogoUrlDark,
    string? FaviconUrl
);

