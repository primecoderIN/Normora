using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Normora.Infrastructure.Hubs;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IWorkspaceStateSynchronizer"/> using distributed cache storage and SignalR
/// pub/sub broadcasting to keep client UI signals continuously in sync with autonomous agent mutations.
/// </summary>
public sealed class WorkspaceStateSynchronizer(
    ICacheService cacheService,
    IHubContext<DocumentHub> hubContext,
    ILogger<WorkspaceStateSynchronizer> logger) : IWorkspaceStateSynchronizer
{
    private static string VersionKey(Guid tenantId) => $"tenant:{tenantId:N}:data_version";

    public async Task<long> GetCurrentVersionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var key = VersionKey(tenantId);
        var version = await cacheService.GetAsync<long?>(key, cancellationToken);
        return version ?? 1L;
    }

    public async Task<long> IncrementVersionAsync(Guid tenantId, string mutationSource, CancellationToken cancellationToken = default)
    {
        var key = VersionKey(tenantId);
        var current = await cacheService.GetAsync<long?>(key, cancellationToken) ?? 1L;
        var newVersion = current + 1L;

        await cacheService.SetAsync(key, newVersion, TimeSpan.FromDays(30), cancellationToken);

        var snapshot = new WorkspaceStateSnapshot(tenantId, newVersion, mutationSource, DateTimeOffset.UtcNow);

        try
        {
            await hubContext.Clients
                .Group(DocumentHub.GroupName(tenantId))
                .SendAsync("WorkspaceStateUpdated", snapshot, cancellationToken);

            logger.LogInformation(
                "Workspace state updated for tenant {TenantId}: version {Version} triggered by '{Source}'",
                tenantId,
                newVersion,
                mutationSource);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast WorkspaceStateUpdated to SignalR group for tenant {TenantId}", tenantId);
        }

        return newVersion;
    }
}

