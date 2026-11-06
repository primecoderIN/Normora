namespace Normora.Modules.Tenants.Application.Branding.DTOs;

/// <summary>
/// Result containing the asset stream and content type.
/// </summary>
public sealed record BrandingAssetResult(Stream Stream, string ContentType);

