using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users.Commands;

/// <summary>
/// Handles <see cref="UpdateUserAssignmentsCommand"/> by synchronizing the user's department and group memberships.
/// </summary>
public class UpdateUserAssignmentsCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateUserAssignmentsCommand, bool>
{
    public async Task<bool> Handle(UpdateUserAssignmentsCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var membership = await dbContext.TenantMemberships
            .Include(m => m.MembershipDepartments)
            .Include(m => m.UserGroupMemberships)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (membership == null)
        {
            throw new BolaException();
        }

        // SEC-16: Validate that all provided DepartmentIds belong to the current tenant
        // to prevent an admin from injecting department references from other tenants.
        var validDepartmentIds = await dbContext.Departments
            .Where(d => d.TenantId == tenantContext.TenantId.Value && request.DepartmentIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        // SEC-16: Validate that all UserGroupIds belong to the current tenant
        var validGroupIds = await dbContext.UserGroups
            .Where(g => g.TenantId == tenantContext.TenantId.Value && request.UserGroupIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        // Sync Departments (only tenant-owned ones)
        membership.MembershipDepartments.Clear();
        foreach (var depId in validDepartmentIds)
        {
            membership.MembershipDepartments.Add(new MembershipDepartment { DepartmentId = depId });
        }

        // Sync User Groups (only tenant-owned ones)
        membership.UserGroupMemberships.Clear();
        foreach (var groupId in validGroupIds)
        {
            membership.UserGroupMemberships.Add(new UserGroupMembership { UserGroupId = groupId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

