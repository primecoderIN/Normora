using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

/// <summary>
/// Query to retrieve a specific user group by its ID within the current tenant.
/// </summary>
public record GetUserGroupByIdQuery(Guid Id) : IRequest<UserGroupDetailDto?>;

/// <summary>
/// Data transfer object representing a user group's detailed information, including member users and assigned departments.
/// </summary>
public record UserGroupDetailDto(
    Guid Id,
    string Name,
    string? Description,
    List<Guid> MemberUserIds,
    List<Guid> DepartmentIds,
    DateTime CreatedAt
);

/// <summary>
/// Handles the <see cref="GetUserGroupByIdQuery"/> by querying the database for a specific user group.
/// </summary>
public class GetUserGroupByIdQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUserGroupByIdQuery, UserGroupDetailDto?>
{
    public async Task<UserGroupDetailDto?> Handle(GetUserGroupByIdQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var group = await dbContext.UserGroups
            .AsNoTracking()
            .Include(g => g.UserGroupMemberships)
                .ThenInclude(m => m.TenantMembership)
            .Include(g => g.UserGroupDepartments)
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null) return null;

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
