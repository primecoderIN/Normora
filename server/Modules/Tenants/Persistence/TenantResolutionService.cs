using Microsoft.EntityFrameworkCore;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>
/// Implements tenant resolution and membership validation by querying the Tenants persistence store.
/// Resolves current membership state from the database so revocations and permission changes take effect
/// immediately across every API replica.
/// </summary>
public sealed class TenantResolutionService(TenantsDbContext dbContext, ICacheService? cache = null) : ITenantResolutionService
{
    // A 30-second sliding TTL absorbs high-frequency request bursts while ensuring role revocations take effect across replicas promptly.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task<ResolvedTenantAccess?> ResolveTenantAccessAsync(
        Guid tenantId,
        string externalUserId,
        CancellationToken cancellationToken = default)
    {
        // Bypass caching during migration/CLI runs or tests when distributed cache is unconfigured.
        if (cache is null)
        {
            return await QueryTenantAccessAsync(tenantId, externalUserId, cancellationToken);
        }

        var cacheKey = $"tenant_access:{tenantId:N}:{externalUserId}";
        return await cache.GetOrCreateAsync(
            cacheKey,
            ct => QueryTenantAccessAsync(tenantId, externalUserId, ct),
            CacheTtl,
            cancellationToken);
    }

    private async Task<ResolvedTenantAccess?> QueryTenantAccessAsync(
        Guid tenantId,
        string externalUserId,
        CancellationToken cancellationToken)
    {
        // Split query avoids Cartesian product multiplication across direct departments and user group navigation collections.
        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Tenant)
            .Include(m => m.User)
            .Include(m => m.MembershipDepartments)
            .Include(m => m.UserGroupMemberships)
                .ThenInclude(ugm => ugm.UserGroup)
                    .ThenInclude(ug => ug.UserGroupDepartments)
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.User.ExternalUserId == externalUserId,
                cancellationToken);

        // Multi-Tenant Isolation & Lifecycle Enforcement:
        // Ensures suspended, archived, or soft-deleted workspaces immediately reject resolution,
        // preventing inactive or delinquent tenants from executing API calls even if memberships exist.
        if (membership is null || membership.Tenant == null || membership.Tenant.Status != Domain.TenantStatus.Active)
        {
            return null;
        }

        // Aggregate effective departmental clearance by unioning direct assignments with group-inherited permissions.
        var directDepartments = membership.MembershipDepartments.Select(md => md.DepartmentId);
        var inheritedDepartments = membership.UserGroupMemberships
            .SelectMany(ugm => ugm.UserGroup.UserGroupDepartments)
            .Select(ugd => ugd.DepartmentId);

        var effectiveDepartments = directDepartments.Concat(inheritedDepartments).Distinct().ToList();

        return new ResolvedTenantAccess(tenantId, membership.Role.ToString(), effectiveDepartments);
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
