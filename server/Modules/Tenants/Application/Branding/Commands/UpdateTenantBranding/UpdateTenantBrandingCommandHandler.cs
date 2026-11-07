using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Branding.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Branding.Commands;

public class UpdateTenantBrandingCommandHandler(
    TenantsDbContext context,
    IBrandingStorageService storageService,
    ICacheService cache
) : IRequestHandler<UpdateTenantBrandingCommand, TenantBrandingDto?>
{
    public async Task<TenantBrandingDto?> Handle(UpdateTenantBrandingCommand request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
            return null;

        var branding = tenant.Branding;
        // Lazily initialize branding record if the workspace is using system defaults.
        if (branding == null)
        {
            branding = new TenantBranding { TenantId = tenant.Id };
            context.TenantBrandings.Add(branding);
        }

        branding.PrimaryColor = request.PrimaryColor;
        branding.SecondaryColor = request.SecondaryColor;
        branding.UpdatedAt = DateTime.UtcNow;

        // Upload custom assets to object storage with cache-busting version query parameters.
        if (request.LogoFile != null)
        {
            var objectName = await storageService.UploadAssetAsync(request.LogoFile, tenant.Slug, "logo");
            branding.LogoUrl = $"/api/tenants/branding/{tenant.Slug}/logo?v={Guid.NewGuid()}";
        }

        if (request.LogoFileDark != null)
        {
            var objectName = await storageService.UploadAssetAsync(request.LogoFileDark, tenant.Slug, "logo-dark");
            branding.LogoUrlDark = $"/api/tenants/branding/{tenant.Slug}/logo-dark?v={Guid.NewGuid()}";
        }

        if (request.FaviconFile != null)
        {
            var objectName = await storageService.UploadAssetAsync(request.FaviconFile, tenant.Slug, "favicon");
            branding.FaviconUrl = $"/api/tenants/branding/{tenant.Slug}/favicon?v={Guid.NewGuid()}";
        }

        await context.SaveChangesAsync(cancellationToken);

        // Instant cache eviction ensures updated theme tokens and logos propagate to browsers immediately.
        await cache.RemoveAsync($"branding:{tenant.Slug.ToLowerInvariant()}", cancellationToken);

        return new TenantBrandingDto(
            tenant.Id,
            tenant.Name,
            branding.PrimaryColor,
            branding.SecondaryColor,
            branding.LogoUrl,
            branding.LogoUrlDark,
            branding.FaviconUrl
        );
    }
}

