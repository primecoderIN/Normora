using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

/// <summary>
/// Command to delete a specific user group from the current tenant.
/// </summary>
public record DeleteUserGroupCommand(Guid Id) : IRequest<bool>, IAuditableRequest
{
    public string Action => "UserGroup.Deleted";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => null;
}


/// <summary>
/// Handles the <see cref="DeleteUserGroupCommand"/> by finding and removing the specified user group.
/// </summary>
public class DeleteUserGroupCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteUserGroupCommand, bool>
{
    public async Task<bool> Handle(DeleteUserGroupCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var group = await dbContext.UserGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            throw new Normora.Shared.Exceptions.BolaException();
        }


        dbContext.UserGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
