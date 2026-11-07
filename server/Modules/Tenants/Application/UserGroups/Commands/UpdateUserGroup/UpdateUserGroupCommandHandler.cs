using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Handles the <see cref="UpdateUserGroupCommand"/> by updating properties and synchronizing department associations.
/// </summary>
public class UpdateUserGroupCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateUserGroupCommand, bool>
{
    public async Task<bool> Handle(UpdateUserGroupCommand request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Validates that this administrative operation operates within an active tenant boundary.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Broken Object Level Authorization (BOLA) Check:
        // Group lookup is strictly scoped to the active TenantId.
        // If the group does not exist or belongs to another tenant, throw BolaException (mapped to 404).
        var group = await dbContext.UserGroups
            .Include(g => g.UserGroupDepartments)
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            throw new BolaException();
        }

        // Case-insensitive name collision check excluding current entity:
        // Prevents renaming a group to a name that already exists within the current tenant workspace.
        var exists = await dbContext.UserGroups
            .AnyAsync(g => g.TenantId == tenantContext.TenantId.Value 
                           && g.Id != request.Id 
                           && g.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.UserGroupExists);
        }

        group.Name = request.Name;
        group.Description = request.Description;

        // BOLA / Cross-Tenant Injection Defense (SEC-17):
        // Validate that all incoming DepartmentIds strictly belong to the current active tenant
        // before performing relational synchronization. This blocks privilege escalation attacks
        // where an admin attempts to associate departments from a different tenant.
        var validDepartmentIds = await dbContext.Departments
            .Where(d => d.TenantId == tenantContext.TenantId.Value && request.DepartmentIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        // Department membership collection reconciliation:
        // Clear existing associations and re-populate with verified tenant-owned department IDs.
        group.UserGroupDepartments.Clear();
        foreach (var depId in validDepartmentIds)
        {
            group.UserGroupDepartments.Add(new UserGroupDepartment { DepartmentId = depId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

