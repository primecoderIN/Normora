using MediatR;
using Normora.Modules.Tenants.Application.Branding.DTOs;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Branding.Queries;

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

