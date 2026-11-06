using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Branding.DTOs;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Branding.Queries;

/// <summary>
/// Handles the <see cref="GetTenantBrandingQuery"/> by fetching the tenant and its branding from the database or cache.
/// </summary>
public class GetTenantBrandingQueryHandler(TenantsDbContext context, ICacheService cache)
    : IRequestHandler<GetTenantBrandingQuery, TenantBrandingDto?>
{
    public async Task<TenantBrandingDto?> Handle(GetTenantBrandingQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"branding:{request.Slug.ToLowerInvariant()}";

        return await cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            // Retrieve the tenant by its unique URL slug to load its custom branding colors and logos
            var tenant = await context.Tenants
                .Include(t => t.Branding)
                .FirstOrDefaultAsync(t => t.Slug == request.Slug && t.Status == Domain.TenantStatus.Active, ct);

            if (tenant == null)
                return null;

            return new TenantBrandingDto(
                tenant.Id,
                tenant.Name,
                tenant.Branding?.PrimaryColor,
                tenant.Branding?.SecondaryColor,
                tenant.Branding?.LogoUrl,
                tenant.Branding?.LogoUrlDark,
                tenant.Branding?.FaviconUrl
            );
        }, TimeSpan.FromMinutes(5), cancellationToken);
    }
}

