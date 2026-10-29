using Microsoft.EntityFrameworkCore;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>
/// Implements tenant resolution and membership validation by querying the Tenants persistence store.
/// Encapsulates direct database access within the Tenants module boundary and caches resolved access
/// to optimize the high-throughput request authentication pipeline.
/// </summary>
public sealed class TenantResolutionService(TenantsDbContext dbContext, ICacheService cache) : ITenantResolutionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public async Task<ResolvedTenantAccess?> ResolveTenantAccessAsync(
        Guid tenantId,
        string externalUserId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant-access:{tenantId}:{externalUserId}";
        var cached = await cache.GetAsync<ResolvedTenantAccess>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(m => m.User)
            .Include(m => m.MembershipDepartments)
            .Include(m => m.UserGroupMemberships)
                .ThenInclude(ugm => ugm.UserGroup)
                    .ThenInclude(ug => ug.UserGroupDepartments)
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.User.ExternalUserId == externalUserId,
                cancellationToken);

        if (membership is null)
        {
            return null;
        }

        var directDepartments = membership.MembershipDepartments.Select(md => md.DepartmentId);
        var inheritedDepartments = membership.UserGroupMemberships
            .SelectMany(ugm => ugm.UserGroup.UserGroupDepartments)
            .Select(ugd => ugd.DepartmentId);

        var effectiveDepartments = directDepartments.Concat(inheritedDepartments).Distinct().ToList();

        var resolved = new ResolvedTenantAccess(tenantId, membership.Role.ToString(), effectiveDepartments);
        await cache.SetAsync(cacheKey, resolved, CacheTtl, cancellationToken);
        return resolved;
    }

    public async Task<bool> HasTenantAccessAsync(
        Guid tenantId,
        string externalUserId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveTenantAccessAsync(tenantId, externalUserId, cancellationToken);
        return resolved is not null;
    }
}
