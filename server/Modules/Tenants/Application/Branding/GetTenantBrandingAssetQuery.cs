using MediatR;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Branding;

/// <summary>
/// Result containing the asset stream and content type.
/// </summary>
public sealed record BrandingAssetResult(Stream Stream, string ContentType);

/// <summary>
/// Query to download a public branding asset for a tenant by slug and asset type.
/// </summary>
public sealed record GetTenantBrandingAssetQuery(string Slug, string AssetType) : IRequest<BrandingAssetResult?>;

/// <summary>
/// Handles downloading branding assets through storage abstraction.
/// </summary>
public sealed class GetTenantBrandingAssetQueryHandler(IBrandingStorageService storageService)
    : IRequestHandler<GetTenantBrandingAssetQuery, BrandingAssetResult?>
{
    public async Task<BrandingAssetResult?> Handle(GetTenantBrandingAssetQuery request, CancellationToken cancellationToken)
    {
        var objectName = $"{request.Slug}/{request.AssetType}";
        var (stream, contentType) = await storageService.DownloadAssetAsync(objectName);
        return new BrandingAssetResult(stream, contentType);
    }
}
