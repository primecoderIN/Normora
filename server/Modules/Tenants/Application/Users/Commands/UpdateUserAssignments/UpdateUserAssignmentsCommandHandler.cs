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
public class UpdateUserAssignmentsCommandHandler(
    TenantsDbContext dbContext,
    ITenantContext tenantContext,
    ICacheService? cache = null)
    : IRequestHandler<UpdateUserAssignmentsCommand, bool>
{
    public async Task<bool> Handle(UpdateUserAssignmentsCommand request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Validates that this membership modification runs within an authenticated tenant boundary.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Broken Object Level Authorization (BOLA) Check:
        // Retrieve the employee's active tenant membership, strictly checking TenantId.
        // Prevents an admin in Tenant A from modifying user assignments for an employee in Tenant B.
        var membership = await dbContext.TenantMemberships
            .Include(m => m.User)
            .Include(m => m.MembershipDepartments)
            .Include(m => m.UserGroupMemberships)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (membership == null)
        {
            throw new BolaException();
        }

        // BOLA / Cross-Tenant Injection Defense (SEC-16):
        // Validate that all supplied DepartmentIds strictly belong to the current active tenant.
        // This prevents an administrator from injecting department references from other tenants,
        // which could lead to unauthorized document access or cross-tenant data leakage.
        var validDepartmentIds = await dbContext.Departments
            .Where(d => d.TenantId == tenantContext.TenantId.Value && request.DepartmentIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        // BOLA / Cross-Tenant Injection Defense (SEC-16):
        // Validate that all supplied UserGroupIds strictly belong to the current active tenant.
        var validGroupIds = await dbContext.UserGroups
            .Where(g => g.TenantId == tenantContext.TenantId.Value && request.UserGroupIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        // Direct Department Associations Synchronization:
        // Overwrite the existing direct department links with the validated tenant-scoped set.
        membership.MembershipDepartments.Clear();
        foreach (var depId in validDepartmentIds)
        {
            membership.MembershipDepartments.Add(new MembershipDepartment { DepartmentId = depId });
        }

        // User Group Membership Synchronization:
        // Overwrite the user's group memberships with the validated tenant-scoped set.
        membership.UserGroupMemberships.Clear();
        foreach (var groupId in validGroupIds)
        {
            membership.UserGroupMemberships.Add(new UserGroupMembership { UserGroupId = groupId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Clearance Cache Invalidation:
        // Evict cached tenant resolution profile to ensure newly assigned/revoked department
        // permissions immediately take effect for the user's subsequent RAG document queries.
        if (cache != null && membership.User != null)
        {
            await cache.RemoveAsync($"tenant_access:{tenantContext.TenantId.Value:N}:{membership.User.ExternalUserId}", cancellationToken);
        }

        return true;
    }
}

