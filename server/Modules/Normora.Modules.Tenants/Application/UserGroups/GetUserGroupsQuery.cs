using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

public record GetUserGroupsQuery : IRequest<List<UserGroupDto>>;

public record UserGroupDto(Guid Id, string Name, List<Guid> DepartmentIds);

public class GetUserGroupsQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUserGroupsQuery, List<UserGroupDto>>
{
    public async Task<List<UserGroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        return await dbContext.UserGroups
            .AsNoTracking()
            .Where(g => g.TenantId == tenantContext.TenantId.Value)
            .Select(g => new UserGroupDto(
                g.Id,
                g.Name,
                g.UserGroupDepartments.Select(d => d.DepartmentId).ToList()
            ))
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }
}
