using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Handles the <see cref="CreateUserGroupCommand"/> by ensuring name uniqueness and saving the group with its department associations.
/// </summary>
public class CreateUserGroupCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<CreateUserGroupCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserGroupCommand request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Validates that the request is running in an authenticated tenant context.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Group Name Uniqueness Check:
        // User groups are RBAC containers grouping employees to assign shared departmental access.
        // Names must be unique within the tenant to prevent ambiguous role assignments.
        var exists = await dbContext.UserGroups
            .AnyAsync(g => g.TenantId == tenantContext.TenantId.Value && g.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.UserGroupExists);
        }

        // BOLA / Cross-Tenant Injection Defense (SEC-17):
        // Filter requested DepartmentIds to ensure ONLY departments actually belonging to this tenant are associated.
        // Any foreign department IDs provided in the payload are stripped, preventing privilege escalation.
        var group = new UserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
            Name = request.Name,
            Description = request.Description,
            UserGroupDepartments = request.DepartmentIds
                .Where(depId => dbContext.Departments
                    .Any(d => d.Id == depId && d.TenantId == tenantContext.TenantId.Value))
                .Select(depId => new UserGroupDepartment { DepartmentId = depId })
                .ToList()
        };

        dbContext.UserGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}

