using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Branding;

/// <summary>
/// Data transfer object containing the branding details for a tenant.
/// </summary>
public record TenantBrandingDto(
    Guid TenantId,
    string TenantName,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    string? FaviconUrl
);

/// <summary>
/// Query to retrieve a tenant's branding information based on their slug.
/// </summary>
/// <param name="Slug">The unique URL slug of the tenant.</param>
public record GetTenantBrandingQuery(string Slug) : IRequest<TenantBrandingDto?>;

/// <summary>
/// Handles the <see cref="GetTenantBrandingQuery"/> by fetching the tenant and its branding from the database.
/// </summary>
public class GetTenantBrandingQueryHandler(TenantsDbContext context)
    : IRequestHandler<GetTenantBrandingQuery, TenantBrandingDto?>
{
    public async Task<TenantBrandingDto?> Handle(GetTenantBrandingQuery request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Slug == request.Slug && t.Status == Domain.TenantStatus.Active, cancellationToken);

        if (tenant == null)
            return null;

        return new TenantBrandingDto(
            tenant.Id,
            tenant.Name,
            tenant.Branding?.PrimaryColor,
            tenant.Branding?.SecondaryColor,
            tenant.Branding?.LogoUrl,
            tenant.Branding?.FaviconUrl
        );
    }
}
