using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.UserGroups.DTOs;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Queries;

/// <summary>
/// Handles the <see cref="GetUserGroupByIdQuery"/> by querying the database for a specific user group.
/// </summary>
public class GetUserGroupByIdQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUserGroupByIdQuery, UserGroupDetailDto>
{
    public async Task<UserGroupDetailDto> Handle(GetUserGroupByIdQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var group = await dbContext.UserGroups
            .AsNoTracking()
            .Include(g => g.UserGroupMemberships)
                .ThenInclude(m => m.TenantMembership)
            .Include(g => g.UserGroupDepartments)
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            throw new BolaException();
        }

        return new UserGroupDetailDto(
            group.Id,
            group.Name,
            group.Description,
            group.UserGroupMemberships.Select(m => m.TenantMembership.UserId).ToList(),
            group.UserGroupDepartments.Select(d => d.DepartmentId).ToList(),
            group.CreatedAt
        );
    }
}

