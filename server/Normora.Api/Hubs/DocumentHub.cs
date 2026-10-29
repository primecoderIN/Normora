using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Normora.Shared.Interfaces;

namespace Normora.Api.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time document processing updates to connected employers.
/// </summary>
[Authorize]
public sealed class DocumentHub(
    ITenantResolutionService tenantResolutionService,
    ICurrentUser currentUser) : Hub
{
    /// <summary>
    /// Securely joins the currently connected client to a tenant-specific notification group.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant the client wants to observe.</param>
    public async Task JoinTenant(Guid tenantId)
    {
        // The tenant ID comes from the client only as a routing request. Membership is
        // revalidated via the tenant boundary service before the connection can receive another tenant's events.
        var isMember = await tenantResolutionService.HasTenantAccessAsync(
            tenantId,
            currentUser.ExternalUserId,
            Context.ConnectionAborted);

        if (!isMember)
        {
            throw new HubException(Normora.Shared.Constants.ApiMessages.HubTenantAccessDenied);
        }


        // Publishers use the same deterministic name, making group membership the sole
        // boundary for tenant-scoped document notifications.
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tenantId));
    }

    /// <summary>
    /// Generates a deterministic SignalR group name for a specific tenant.
    /// </summary>
    public static string GroupName(Guid tenantId) => $"tenant:{tenantId:N}";
}

/// <summary>
/// Represents an event payload sent to the client when a document's status changes.
/// </summary>
public sealed record DocumentStatusChanged(
    Guid DocumentId,
    Guid TenantId,
    string FileName,
    string Status);