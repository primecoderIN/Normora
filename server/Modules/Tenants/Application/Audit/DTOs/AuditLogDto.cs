namespace Normora.Modules.Tenants.Application.Audit.DTOs;

/// <summary>
/// Data transfer object representing a historical tenant security/audit record.
/// Projected from <see cref="Domain.AuditLog"/> entities for UI compliance grids and CSV exports.
/// </summary>
public record AuditLogDto(
    // Unique identifier of the audit log record.
    Guid Id,

    // External subject ID (OIDC 'sub') of the user who initiated the operation.
    string? ActorUserId,

    // Snapshot of the actor's email address captured at the exact moment of execution.
    string? ActorEmail,

    // Snapshot of the actor's display name captured at the exact moment of execution.
    string? ActorName,

    // Standardized dot-notation event action (e.g. "Employee.Invited", "Branding.Updated").
    string Action,

    // Domain entity type affected by the operation (e.g. "TenantMembership", "TenantBranding").
    string? TargetType,

    // Primary key identifier of the affected domain entity.
    string? TargetId,

    // Serialized JSON payload containing relevant context diffs or modified properties.
    string? Metadata,

    // UTC timestamp when the audit event was recorded by the MediatR AuditBehavior pipeline.
    DateTime OccurredAt
);

