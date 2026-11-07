using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Tenants.Commands;

/// <summary>
/// Handles <see cref="SuspendTenantCommand"/> by setting the tenant's status to Suspended.
/// </summary>
public class SuspendTenantCommandHandler(TenantsDbContext dbContext) : IRequestHandler<SuspendTenantCommand, bool>
{
    public async Task<bool> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        // Tenant Lifecycle & Access Suspension Workflow:
        // Retrieves the workspace entity by Primary Key (request.TenantId).
        // If not found, returns false indicating no-op.
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
        
        if (tenant == null)
            return false;

        // Suspension Invariant:
        // Transitioning status to TenantStatus.Suspended immediately blocks tenant access in
        // TenantResolutionMiddleware for all employees and administrators of this organization,
        // while safely preserving all historical documents, embeddings, and chat history.
        tenant.Status = TenantStatus.Suspended;
        tenant.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
