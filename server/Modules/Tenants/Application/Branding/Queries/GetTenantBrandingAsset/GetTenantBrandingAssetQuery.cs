using MediatR;
using Normora.Modules.Tenants.Application.Branding.DTOs;

namespace Normora.Modules.Tenants.Application.Branding.Queries;

/// <summary>
/// Query to download a public branding asset for a tenant by slug and asset type.
/// </summary>
public sealed record GetTenantBrandingAssetQuery(string Slug, string AssetType) : IRequest<BrandingAssetResult?>;

