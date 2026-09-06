using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

/// <summary>
/// Query to retrieve a list of all user groups for the current tenant.
/// </summary>
public record GetUserGroupsQuery : IRequest<List<UserGroupDto>>;

/// <summary>
/// Data transfer object representing a user group and its associated departments.
/// </summary>
public record UserGroupDto(Guid Id, string Name, List<Guid> DepartmentIds);

/// <summary>
/// Handles the <see cref="GetUserGroupsQuery"/> by querying the database for the current tenant's user groups.
/// </summary>
public class GetUserGroupsQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUserGroupsQuery, List<UserGroupDto>>
{
    public async Task<List<UserGroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        return await dbContext.UserGroups
            .AsNoTracking()
            .Where(g => g.TenantId == tenantContext.TenantId.Value)
            .OrderBy(g => g.Name)
            .Select(g => new UserGroupDto(
                g.Id,
                g.Name,
                g.UserGroupDepartments.Select(d => d.DepartmentId).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}
