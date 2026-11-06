namespace Normora.Modules.Tenants.Application.Users.DTOs;

/// <summary>
/// Data transfer object representing the department and user group assignments for a user.
/// </summary>
public record UserAssignmentsDto(Guid UserId, List<Guid> DepartmentIds, List<Guid> UserGroupIds);

