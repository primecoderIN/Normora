using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.SuspendTenant;

public class SuspendTenantCommandHandler(TenantsDbContext dbContext) : IRequestHandler<SuspendTenantCommand, bool>
{
    public async Task<bool> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
        
        if (tenant == null)
            return false;

        tenant.Status = TenantStatus.Suspended;
        tenant.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
