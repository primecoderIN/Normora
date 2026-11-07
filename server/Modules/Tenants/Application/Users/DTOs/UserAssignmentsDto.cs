namespace Normora.Modules.Tenants.Application.Users.DTOs;

/// <summary>
/// Data transfer object representing the department and user group assignments for a user.
/// Used to populate the employee assignment modal in the employer portal.
/// </summary>
public record UserAssignmentsDto(
    // Target user identifier.
    Guid UserId,

    // Active directly assigned department IDs.
    List<Guid> DepartmentIds,

    // Active user security group IDs.
    List<Guid> UserGroupIds
);

