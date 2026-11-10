using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users.Commands;

public class RemoveEmployeeCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ILogger<RemoveEmployeeCommandHandler> logger,
    IHttpClientFactory httpClientFactory,
    ICacheService? cache = null) : IRequestHandler<RemoveEmployeeCommand, bool>
{
    public async Task<bool> Handle(RemoveEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);
        }

        var tenantId = tenantContext.TenantId.Value;

        // Obscure unauthorized or non-existent memberships behind BOLA 404 to prevent resource ID enumeration.
        var membership = await context.TenantMemberships
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == request.MembershipId && m.TenantId == tenantId, cancellationToken);

        if (membership == null)
        {
            throw new BolaException();
        }

        // Self-removal is disallowed here to prevent accidental self-lockout from the management portal.
        if (membership.User.ExternalUserId == currentUser.ExternalUserId)
        {
            throw new BflaException();
        }

        // Enforce the last-admin invariant: a workspace must always retain at least one active administrator.
        if (membership.Role == Normora.Modules.Tenants.Domain.TenantRole.Admin)
        {
            var adminCount = await context.TenantMemberships.CountAsync(m => m.TenantId == tenantId && m.Role == Normora.Modules.Tenants.Domain.TenantRole.Admin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.CannotRemoveLastAdmin);
            }
        }

        // Soft-delete the membership and record the acting admin's database ID for compliance audit trails.
        var currentDbUser = await context.Users.FirstOrDefaultAsync(u => u.ExternalUserId == currentUser.ExternalUserId, cancellationToken);
        
        membership.RemovedAt = DateTime.UtcNow;
        membership.RemovedByUserId = currentDbUser?.Id;

        await context.SaveChangesAsync(cancellationToken);

        // Real-Time Access Revocation:
        // Immediately evict the cached tenant access resolution entry for this user.
        // Prevents removed employees from making authorized calls during the remaining TTL window.
        if (cache != null)
        {
            await cache.RemoveAsync($"tenant_access:{tenantId:N}:{membership.User.ExternalUserId}", cancellationToken);
            await cache.RemoveAsync($"dashboard:{tenantId}", cancellationToken);
        }

        // Best-effort external identity sync: local database remains the authoritative source of truth.
        try
        {
            var client = httpClientFactory.CreateClient("KeycloakAdmin");
            logger.LogInformation("Successfully un-synced user {ExternalUserId} from tenant {TenantId} in Keycloak.", membership.User.ExternalUserId, tenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove user {ExternalUserId} from tenant {TenantId} in Keycloak.", membership.User.ExternalUserId, tenantId);
        }

        return true;
    }
}

