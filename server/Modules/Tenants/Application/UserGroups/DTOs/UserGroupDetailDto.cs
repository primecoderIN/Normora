namespace Normora.Modules.Tenants.Application.UserGroups.DTOs;

/// <summary>
/// Data transfer object representing the complete configuration of a user security group.
/// Supplies active membership rosters and departmental permission mappings for group editing views.
/// </summary>
public record UserGroupDetailDto(
    // Unique identifier of the user group.
    Guid Id,

    // Display name of the user group.
    string Name,

    // Optional functional description.
    string? Description,

    // List of internal UserIds currently enrolled as active members of this group.
    List<Guid> MemberUserIds,

    // List of DepartmentIds currently linked to this group.
    List<Guid> DepartmentIds,

    // UTC timestamp when the user group was originally created.
    DateTime CreatedAt
);

