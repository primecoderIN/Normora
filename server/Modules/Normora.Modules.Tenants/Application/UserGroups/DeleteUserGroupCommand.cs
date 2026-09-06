using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

public record DeleteUserGroupCommand(Guid Id) : IRequest<bool>;

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
