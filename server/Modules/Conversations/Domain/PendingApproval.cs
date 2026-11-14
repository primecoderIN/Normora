using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Status of a pending autonomous agent action awaiting human-in-the-loop authorization.
/// </summary>
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Dismissed = 2,
    Expired = 3
}

/// <summary>
/// Represents a human-in-the-loop (HITL) approval gate for high-risk or destructive agent tool operations.
/// Prevents unconfirmed autonomous writes/deletes by persisting the intended execution payload until the user explicitly confirms or dismisses.
/// </summary>
public class PendingApproval : ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>Tenant identifier for strict tenant boundary isolation.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Associated conversation thread identifier where tool calling occurred, if applicable.</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>User ID who requested or triggered the action.</summary>
    public Guid UserId { get; set; }

    /// <summary>The unique identifier of the tool requiring authorization (e.g. documents_delete).</summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>The serialized JSON parameters to execute upon approval.</summary>
    public string ArgumentsJson { get; set; } = string.Empty;

    /// <summary>Optional snapshot of affected resource state prior to execution.</summary>
    public string? SnapshotJson { get; set; }

    /// <summary>Current lifecycle status of the approval request.</summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>Timestamp when the approval was requested.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Timestamp after which this approval can no longer be executed.</summary>
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(24);

    /// <summary>Timestamp when the approval was confirmed and executed.</summary>
    public DateTimeOffset? ExecutedAt { get; set; }

    /// <summary>The execution output or result payload returned upon confirmation.</summary>
    public string? ExecutionOutputJson { get; set; }
}

