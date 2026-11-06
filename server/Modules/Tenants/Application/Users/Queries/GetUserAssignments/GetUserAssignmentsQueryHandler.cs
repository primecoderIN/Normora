using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Users.DTOs;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Handles <see cref="GetUserAssignmentsQuery"/> by loading the membership record with its department and group associations.
/// </summary>
public class GetUserAssignmentsQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetUserAssignmentsQuery, UserAssignmentsDto>
{
    public async Task<UserAssignmentsDto> Handle(GetUserAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var membership = await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(m => m.MembershipDepartments)
            .Include(m => m.UserGroupMemberships)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (membership == null)
        {
            throw new BolaException();
        }

        return new UserAssignmentsDto(
            request.UserId,
            membership.MembershipDepartments.Select(md => md.DepartmentId).ToList(),
            membership.UserGroupMemberships.Select(ugm => ugm.UserGroupId).ToList()
        );
    }
}

