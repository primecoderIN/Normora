namespace Normora.Modules.Tenants.Application.UserGroups.DTOs;

/// <summary>
/// Lightweight data transfer object representing a user group summary for list grids.
/// </summary>
public record UserGroupDto(
    // Unique identifier of the user group.
    Guid Id,

    // Display name of the user group.
    string Name,

    // Optional functional description.
    string? Description,

    // Number of active employee members currently enrolled in this group.
    int MemberCount,

    // Number of organizational departments linked to this group.
    int DepartmentCount,

    // UTC timestamp when the user group was originally created.
    DateTime CreatedAt,

    // List of DepartmentIds assigned to this group for fast client-side chip rendering.
    List<Guid> DepartmentIds
);

