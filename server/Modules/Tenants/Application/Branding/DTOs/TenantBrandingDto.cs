namespace Normora.Modules.Tenants.Application.Branding.DTOs;

/// <summary>
/// Data transfer object containing the public white-label branding details for a tenant.
/// Consumed by the Angular client to dynamically inject CSS variables (--primary-color, --secondary-color)
/// and render customized organizational logos across the header, login page, and browser tab.
/// </summary>
public record TenantBrandingDto(
    // Unique identifier of the tenant.
    Guid TenantId,

    // Display name of the organization.
    string TenantName,

    // Primary brand color in CSS hex format (e.g. "#2563EB").
    string? PrimaryColor,

    // Accent/secondary brand color in CSS hex format.
    string? SecondaryColor,

    // Public URL or BFF proxy route delivering the light-mode organization logo.
    string? LogoUrl,

    // Public URL or BFF proxy route delivering the dark-mode optimized logo variant.
    string? LogoUrlDark,

    // Public URL or BFF proxy route delivering the browser tab icon.
    string? FaviconUrl
);

