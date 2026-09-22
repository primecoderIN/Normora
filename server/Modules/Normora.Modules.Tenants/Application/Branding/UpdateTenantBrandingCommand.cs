using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Branding;

public record UpdateTenantBrandingCommand(
    Guid TenantId,
    string? PrimaryColor,
    string? SecondaryColor,
    IFormFile? LogoFile,
    IFormFile? LogoFileDark,
    IFormFile? FaviconFile
) : IRequest<TenantBrandingDto?>;

public class UpdateTenantBrandingCommandHandler(
    TenantsDbContext context,
    IBrandingStorageService storageService
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
        if (branding == null)
        {
            branding = new TenantBranding { TenantId = tenant.Id };
            context.TenantBrandings.Add(branding);
        }

        branding.PrimaryColor = request.PrimaryColor;
        branding.SecondaryColor = request.SecondaryColor;
        branding.UpdatedAt = DateTime.UtcNow;

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
