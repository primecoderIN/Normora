using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.UserGroups.DTOs;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Queries;

/// <summary>
/// Handles the <see cref="GetUserGroupsQuery"/> by querying the database for the current tenant's user groups.
/// </summary>
public class GetUserGroupsQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUserGroupsQuery, List<UserGroupDto>>
{
    public async Task<List<UserGroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Ensures user group retrieval is strictly restricted to the ambient tenant workspace.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Aggregated Group Projection:
        // Projects user groups alongside related member counts and linked department counts.
        // Uses AsNoTracking() to bypass EF change tracking overhead on read operations.
        return await dbContext.UserGroups
            .AsNoTracking()
            .Where(g => g.TenantId == tenantContext.TenantId.Value)
            .OrderBy(g => g.Name)
            .Select(g => new UserGroupDto(
                g.Id,
                g.Name,
                g.Description,
                g.UserGroupMemberships.Count,
                g.UserGroupDepartments.Count,
                g.CreatedAt,
                g.UserGroupDepartments.Select(d => d.DepartmentId).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}

