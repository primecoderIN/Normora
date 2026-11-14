namespace Normora.Shared.Interfaces;

/// <summary>
/// Snapshot of workspace state containing the latest monotonic version number and mutation origin.
/// </summary>
public sealed record WorkspaceStateSnapshot(
    Guid TenantId,
    long DataVersion,
    string MutationSource,
    DateTimeOffset Timestamp);

/// <summary>
/// Service interface for managing workspace data versions and broadcasting state synchronization events
/// across distributed nodes and connected web clients.
/// </summary>
public interface IWorkspaceStateSynchronizer
{
    /// <summary>
    /// Gets the current monotonic data version for the specified workspace.
    /// </summary>
    Task<long> GetCurrentVersionAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments the workspace data version and broadcasts the change via SignalR.
    /// </summary>
    Task<long> IncrementVersionAsync(Guid tenantId, string mutationSource, CancellationToken cancellationToken = default);
}

