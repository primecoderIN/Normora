namespace Normora.Shared.Interfaces;

/// <summary>
/// Service contract for requesting and verifying human-in-the-loop (HITL) approval gates.
/// Intercepts destructive or high-risk autonomous agent tool operations.
/// </summary>
public interface IAgentApprovalService
{
    /// <summary>
    /// Enqueues a pending approval request for a tool call requiring explicit user confirmation.
    /// </summary>
    /// <param name="tenantId">Active workspace tenant ID.</param>
    /// <param name="userId">Executing user's external or local user ID.</param>
    /// <param name="conversationId">Associated conversation ID, if initiated from a chat turn.</param>
    /// <param name="toolName">The unique identifier of the tool requiring authorization.</param>
    /// <param name="argumentsJson">The serialized JSON parameters to execute upon approval.</param>
    /// <param name="snapshotJson">Optional snapshot of affected resource state prior to execution.</param>
    /// <param name="ttl">Optional lifespan after which the approval expires (defaults to 24 hours).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A unique approval ID (<see cref="Guid"/>) to embed into the action card.</returns>
    Task<Guid> RequestApprovalAsync(
        Guid tenantId,
        string userId,
        Guid? conversationId,
        string toolName,
        string argumentsJson,
        string? snapshotJson = null,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);
}

