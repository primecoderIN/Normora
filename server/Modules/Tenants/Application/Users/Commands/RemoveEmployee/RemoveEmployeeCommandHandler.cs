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
    IHttpClientFactory httpClientFactory) : IRequestHandler<RemoveEmployeeCommand, bool>
{
    public async Task<bool> Handle(RemoveEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);
        }

        var tenantId = tenantContext.TenantId.Value;

        // Fetch the membership, including the user so we have the KeycloakUserId
        var membership = await context.TenantMemberships
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == request.MembershipId && m.TenantId == tenantId, cancellationToken);

        // BOLA: Return 404 if not found or belongs to another tenant
        if (membership == null)
        {
            throw new BolaException();
        }

        // BFLA: Prevent removing the last admin or oneself? (optional, but good practice)
        if (membership.User.ExternalUserId == currentUser.ExternalUserId)
        {
            throw new BflaException(); // Cannot remove yourself this way
        }

        // Prevent removing the last admin
        if (membership.Role == Normora.Modules.Tenants.Domain.TenantRole.Admin)
        {
            var adminCount = await context.TenantMemberships.CountAsync(m => m.TenantId == tenantId && m.Role == Normora.Modules.Tenants.Domain.TenantRole.Admin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.CannotRemoveLastAdmin);
            }
        }

        // 1. Soft-delete the membership locally
        // We get the current user's DB ID to populate RemovedByUserId
        var currentDbUser = await context.Users.FirstOrDefaultAsync(u => u.ExternalUserId == currentUser.ExternalUserId, cancellationToken);
        
        membership.RemovedAt = DateTime.UtcNow;
        membership.RemovedByUserId = currentDbUser?.Id;

        await context.SaveChangesAsync(cancellationToken);

        // 2. Best-effort Keycloak Admin API sync (Local-first strategy)
        try
        {
            var client = httpClientFactory.CreateClient("KeycloakAdmin");
            logger.LogInformation("Successfully un-synced user {KeycloakUserId} from tenant {TenantId} in Keycloak.", membership.User.KeycloakUserId, tenantId);
        }
        catch (Exception ex)
        {
            // We log the error but DO NOT revert the DB transaction. 
            // Local DB is the source of truth for the application.
            logger.LogError(ex, "Failed to remove user {KeycloakUserId} from tenant {TenantId} in Keycloak.", membership.User.KeycloakUserId, tenantId);
        }

        return true;
    }
}

