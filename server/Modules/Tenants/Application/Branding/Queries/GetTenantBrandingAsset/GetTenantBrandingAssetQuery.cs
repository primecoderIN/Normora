using MediatR;
using Normora.Modules.Tenants.Application.Branding.DTOs;

namespace Normora.Modules.Tenants.Application.Branding.Queries;

/// <summary>
/// Query to stream a public branding asset (light logo, dark logo, or favicon) for a tenant by slug and asset type.
/// Handled by <see cref="GetTenantBrandingAssetQueryHandler"/>.
/// </summary>
/// <remarks>
/// Proxy Streaming:
/// - Provides public access to tenant logos without generating short-lived presigned URLs.
/// - Validates asset type strings ("logo", "logo-dark", "favicon") to prevent arbitrary storage key traversal.
/// </remarks>
public sealed record GetTenantBrandingAssetQuery(
    // The unique vanity slug of the tenant.
    string Slug,

    // The requested asset identifier: "logo", "logo-dark", or "favicon".
    string AssetType
) : IRequest<BrandingAssetResult?>;

