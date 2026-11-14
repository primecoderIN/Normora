namespace Normora.Modules.Conversations.Application.Approvals.DTOs;

/// <summary>
/// Detailed representation of a pending autonomous agent action awaiting authorization.
/// </summary>
public sealed record PendingApprovalDto(
    Guid Id,
    Guid? ConversationId,
    Guid UserId,
    string ToolName,
    string ArgumentsJson,
    string? SnapshotJson,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ExecutedAt);

/// <summary>
/// Result returned when a pending approval is executed following human authorization.
/// </summary>
public sealed record ConfirmApprovalResultDto(
    bool Success,
    string ToolName,
    string? Output,
    string? ErrorMessage,
    bool ChangedData,
    object? StructuredData);

/// <summary>
/// Result returned when a pending approval is dismissed/cancelled.
/// </summary>
public sealed record DismissApprovalResultDto(
    bool Success,
    string Message);

