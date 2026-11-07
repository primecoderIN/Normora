using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Branding.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Branding.Commands;

public class UpdateTenantBrandingCommandHandler(
    TenantsDbContext context,
    IBrandingStorageService storageService,
    ICacheService cache
) : IRequestHandler<UpdateTenantBrandingCommand, TenantBrandingDto?>
{
    private const long MaxAssetSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<TenantBrandingDto?> Handle(UpdateTenantBrandingCommand request, CancellationToken cancellationToken)
    {
        // Deep Inspection of Uploaded Branding Assets to prevent Stored XSS and payload exploits
        ValidateAsset(request.LogoFile);
        ValidateAsset(request.LogoFileDark);
        ValidateAsset(request.FaviconFile);

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

    /// <summary>
    /// Performs deep content inspection on uploaded branding assets to prevent Stored XSS and arbitrary payload uploads:
    /// 1. File size enforcement (max 5MB).
    /// 2. Extension allowlist strictly limited to image types (.png, .jpg, .jpeg, .webp, .ico).
    /// 3. Binary magic bytes header inspection ensuring actual byte content matches declared extension.
    /// Note: SVG uploads are intentionally excluded to eliminate inline script execution vectors in tenant contexts.
    /// </summary>
    private static void ValidateAsset(IFormFile? file)
    {
        if (file == null) return;

        if (file.Length > MaxAssetSizeBytes)
        {
            throw new InvalidOperationException(ApiMessages.BrandingFileExceedsLimit);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".ico";
        if (!allowed)
        {
            throw new InvalidOperationException(ApiMessages.InvalidBrandingFileType);
        }

        if (file.Length < 4)
        {
            throw new InvalidOperationException(ApiMessages.InvalidFileContent);
        }

        Span<byte> header = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        var bytesRead = stream.Read(header);
        if (bytesRead < 4)
        {
            throw new InvalidOperationException(ApiMessages.InvalidFileContent);
        }

        var validMagicBytes = ext switch
        {
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46,
            ".ico" => header[0] == 0x00 && header[1] == 0x00 && header[2] == 0x01 && header[3] == 0x00,
            _ => false
        };

        if (!validMagicBytes)
        {
            throw new InvalidOperationException(ApiMessages.InvalidFileContent);
        }
    }
}

