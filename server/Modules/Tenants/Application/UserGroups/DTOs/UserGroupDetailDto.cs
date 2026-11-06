namespace Normora.Modules.Tenants.Application.UserGroups.DTOs;

/// <summary>
/// Data transfer object representing a user group's detailed information, including member users and assigned departments.
/// </summary>
public record UserGroupDetailDto(
    Guid Id,
    string Name,
    string? Description,
    List<Guid> MemberUserIds,
    List<Guid> DepartmentIds,
    DateTime CreatedAt
);

