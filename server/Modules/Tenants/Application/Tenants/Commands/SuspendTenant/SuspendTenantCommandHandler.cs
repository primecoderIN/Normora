using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Tenants.Commands;

/// <summary>
/// Handles <see cref="SuspendTenantCommand"/> by setting the tenant's status to Suspended
/// and invalidating real-time cached tenant access across all cluster nodes.
/// </summary>
public class SuspendTenantCommandHandler(TenantsDbContext dbContext, ICacheService? cache = null) : IRequestHandler<SuspendTenantCommand, bool>
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

        // Instantaneous Cluster-Wide Cache Eviction:
        // Purges all cached session access entries for this tenant so active sessions cannot continue
        // executing authenticated operations during the sliding cache TTL window.
        if (cache is not null)
        {
            var userIds = await dbContext.TenantMemberships
                .Where(m => m.TenantId == request.TenantId)
                .Select(m => m.User.ExternalUserId)
                .ToListAsync(cancellationToken);

            foreach (var userId in userIds)
            {
                await cache.RemoveAsync($"tenant_access:{request.TenantId:N}:{userId}", cancellationToken);
            }
        }

        return true;
    }
}
