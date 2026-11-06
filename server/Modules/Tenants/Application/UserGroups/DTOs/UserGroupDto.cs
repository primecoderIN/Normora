namespace Normora.Modules.Tenants.Application.UserGroups.DTOs;

/// <summary>
/// Data transfer object representing a user group and its associated departments.
/// </summary>
public record UserGroupDto(
    Guid Id,
    string Name,
    string? Description,
    int MemberCount,
    int DepartmentCount,
    DateTime CreatedAt,
    List<Guid> DepartmentIds
);

