using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Handles the <see cref="DeleteUserGroupCommand"/> by finding and removing the specified user group.
/// </summary>
public class DeleteUserGroupCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteUserGroupCommand, bool>
{
    public async Task<bool> Handle(DeleteUserGroupCommand request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Ensures the request has a valid tenant context before executing deletion.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Broken Object Level Authorization (BOLA) Check:
        // Ensures administrators can only delete user groups strictly within their active tenant.
        var group = await dbContext.UserGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            throw new BolaException();
        }

        // Relational Cascade Cleanup:
        // Removing the UserGroup entity cascades through EF Core configuration:
        // 1. UserGroupMemberships (associations of employees to this group) are removed.
        // 2. UserGroupDepartments (associations of this group to departments) are removed.
        // The underlying User entities and Department entities are untouched.
        dbContext.UserGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

