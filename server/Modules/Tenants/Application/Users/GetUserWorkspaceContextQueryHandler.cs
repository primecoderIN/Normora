using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Contracts.Tenants;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Handles GetUserWorkspaceContextQuery by querying TenantsDbContext within the Tenants module boundary.
/// </summary>
public sealed class GetUserWorkspaceContextQueryHandler(TenantsDbContext dbContext)
    : IRequestHandler<GetUserWorkspaceContextQuery, UserWorkspaceContextResult>
{
    public async Task<UserWorkspaceContextResult> Handle(
        GetUserWorkspaceContextQuery request,
        CancellationToken cancellationToken)
    {
        var memberships = await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(m => m.Tenant)
            .Where(m => m.User.ExternalUserId == request.ExternalUserId)
            .ToListAsync(cancellationToken);

        var personalTenantId = memberships.FirstOrDefault(m => m.Tenant.IsPersonal)?.TenantId;
        var activeTenant = memberships.FirstOrDefault(m => m.TenantId == request.ActiveTenantId)?.Tenant;

        var tenantName = activeTenant?.Name ?? "your organization";
        var tenantSlug = activeTenant?.Slug;

        return new UserWorkspaceContextResult(personalTenantId, tenantName, tenantSlug);
    }
}
