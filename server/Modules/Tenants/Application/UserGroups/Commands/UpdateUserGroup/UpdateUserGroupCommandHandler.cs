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
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var group = await dbContext.UserGroups
            .Include(g => g.UserGroupDepartments)
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            throw new BolaException();
        }

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

        // SEC-17: Validate that all provided DepartmentIds belong to the current tenant
        // before syncing to prevent cross-tenant department injection.
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

