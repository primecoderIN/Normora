using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users;

public record GetUserAssignmentsQuery(Guid UserId) : IRequest<UserAssignmentsDto?>;

public record UserAssignmentsDto(Guid UserId, List<Guid> DepartmentIds, List<Guid> UserGroupIds);

public class GetUserAssignmentsQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUserAssignmentsQuery, UserAssignmentsDto?>
{
    public async Task<UserAssignmentsDto?> Handle(GetUserAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(m => m.MembershipDepartments)
            .Include(m => m.UserGroupMemberships)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (membership == null)
        {
            return null;
        }

        return new UserAssignmentsDto(
            request.UserId,
            membership.MembershipDepartments.Select(md => md.DepartmentId).ToList(),
            membership.UserGroupMemberships.Select(ugm => ugm.UserGroupId).ToList()
        );
    }
}
