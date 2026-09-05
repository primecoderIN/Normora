using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Api.Hubs;

[Authorize]
public sealed class DocumentHub(
    TenantsDbContext tenantsDbContext,
    ICurrentUser currentUser) : Hub
{
    public async Task JoinTenant(Guid tenantId)
    {
        // The tenant ID comes from the client only as a routing request. Membership is
        // revalidated here before the connection can receive another tenant's events.
        var isMember = await tenantsDbContext.TenantMemberships
            .Include(membership => membership.User)
            .AnyAsync(membership =>
                membership.TenantId == tenantId &&
                membership.User.KeycloakUserId == currentUser.KeycloakUserId);

        if (!isMember)
        {
            throw new HubException("You do not have access to this tenant.");
        }

        // Publishers use the same deterministic name, making group membership the sole
        // boundary for tenant-scoped document notifications.
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tenantId));
    }

    public static string GroupName(Guid tenantId) => $"tenant:{tenantId:N}";
}

public sealed record DocumentStatusChanged(
    Guid DocumentId,
    Guid TenantId,
    string FileName,
    string Status);