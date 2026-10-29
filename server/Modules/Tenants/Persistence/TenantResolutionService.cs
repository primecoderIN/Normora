using Microsoft.EntityFrameworkCore;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>
/// Implements tenant resolution and membership validation by querying the Tenants persistence store.
/// Resolves current membership state from the database so revocations and permission changes take effect
/// immediately across every API replica.
/// </summary>
public sealed class TenantResolutionService(TenantsDbContext dbContext) : ITenantResolutionService
{
    public async Task<ResolvedTenantAccess?> ResolveTenantAccessAsync(
        Guid tenantId,
        string externalUserId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .AsSplitQuery()
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
