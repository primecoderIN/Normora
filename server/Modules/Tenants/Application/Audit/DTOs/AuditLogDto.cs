namespace Normora.Modules.Tenants.Application.Audit.DTOs;

/// <summary>
/// Data transfer object representing a historical tenant security/audit record.
/// </summary>
public record AuditLogDto(
    Guid Id,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorName,
    string Action,
    string? TargetType,
    string? TargetId,
    string? Metadata,
    DateTime OccurredAt
);

