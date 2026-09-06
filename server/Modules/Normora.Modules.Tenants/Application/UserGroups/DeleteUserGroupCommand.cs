using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

/// <summary>
/// Command to delete a specific user group from the current tenant.
/// </summary>
/// <param name="Id">The unique identifier of the user group to delete.</param>
public record DeleteUserGroupCommand(Guid Id) : IRequest<bool>;

/// <summary>
/// Handles the <see cref="DeleteUserGroupCommand"/> by finding and removing the specified user group.
/// </summary>
public class DeleteUserGroupCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteUserGroupCommand, bool>
{
    public async Task<bool> Handle(DeleteUserGroupCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var group = await dbContext.UserGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            return false;
        }

        dbContext.UserGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
