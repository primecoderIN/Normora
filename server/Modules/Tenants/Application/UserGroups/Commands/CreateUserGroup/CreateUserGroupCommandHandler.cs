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
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var exists = await dbContext.UserGroups
            .AnyAsync(g => g.TenantId == tenantContext.TenantId.Value && g.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.UserGroupExists);
        }

        var group = new UserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
            Name = request.Name,
            Description = request.Description,
            // SEC-17: Only link departments that actually belong to this tenant
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

