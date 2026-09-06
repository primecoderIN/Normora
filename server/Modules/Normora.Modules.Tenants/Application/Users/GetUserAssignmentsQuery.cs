using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Query to retrieve the department and user group assignments for a specific user within the current tenant.
/// </summary>
/// <param name="UserId">The ID of the user whose assignments are to be retrieved.</param>
public record GetUserAssignmentsQuery(Guid UserId) : IRequest<UserAssignmentsDto?>;

/// <summary>
/// Data transfer object representing the department and user group assignments for a user.
/// </summary>
public record UserAssignmentsDto(Guid UserId, List<Guid> DepartmentIds, List<Guid> UserGroupIds);

/// <summary>
/// Handles <see cref="GetUserAssignmentsQuery"/> by loading the membership record with its department and group associations.
/// </summary>
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
