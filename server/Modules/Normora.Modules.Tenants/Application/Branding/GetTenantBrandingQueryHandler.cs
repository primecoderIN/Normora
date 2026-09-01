using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Branding;

public record TenantBrandingDto(
    Guid TenantId,
    string TenantName,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    string? FaviconUrl
);

public record GetTenantBrandingQuery(string Slug) : IRequest<TenantBrandingDto?>;

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
