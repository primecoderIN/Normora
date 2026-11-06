using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Handles the <see cref="UpdateUserGroupAssignmentsCommand"/> by synchronizing member and department associations.
/// </summary>
public class UpdateUserGroupAssignmentsCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateUserGroupAssignmentsCommand, bool>
{
    public async Task<bool> Handle(UpdateUserGroupAssignmentsCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var group = await dbContext.UserGroups
            .Include(g => g.UserGroupMemberships)
            .Include(g => g.UserGroupDepartments)
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            throw new BolaException();
        }

        // Resolve user IDs to tenant membership IDs
        var membershipIds = await dbContext.TenantMemberships
            .Where(tm => tm.TenantId == tenantContext.TenantId.Value && request.MemberUserIds.Contains(tm.UserId))
            .Select(tm => tm.Id)
            .ToListAsync(cancellationToken);

        // Sync members (already validated via TenantMembership join above)
        group.UserGroupMemberships.Clear();
        foreach (var memId in membershipIds)
        {
            group.UserGroupMemberships.Add(new UserGroupMembership { TenantMembershipId = memId });
        }

        // SEC-17: Validate that all provided DepartmentIds belong to the current tenant
        var validDepartmentIds = await dbContext.Departments
            .Where(d => d.TenantId == tenantContext.TenantId.Value && request.DepartmentIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        // Sync departments (only tenant-owned ones)
        group.UserGroupDepartments.Clear();
        foreach (var depId in validDepartmentIds)
        {
            group.UserGroupDepartments.Add(new UserGroupDepartment { DepartmentId = depId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

