namespace Normora.Modules.Tenants.Application.Users.DTOs;

/// <summary>
/// Enriched employee profile containing membership ID, user ID, role, and department/group associations.
/// Displayed on the Employer Directory grid and employee drawer views.
/// </summary>
public record TenantEmployeeDto(
    // Unique identifier of the tenant membership record.
    Guid MembershipId,

    // Internal unique identifier of the user identity.
    Guid UserId,

    // Verified email address of the employee.
    string Email,

    // Display name of the employee.
    string DisplayName,

    // Role within the organization ("Owner", "Admin", "Employee").
    string Role,

    // UTC timestamp when the employee joined the workspace.
    DateTime JoinedAt,

    // List of departments the employee is directly assigned to.
    IReadOnlyList<DepartmentSummaryDto> Departments,

    // List of user security groups the employee belongs to.
    IReadOnlyList<UserGroupSummaryDto> UserGroups
);

/// <summary>
/// Lightweight department identifier and name tuple.
/// </summary>
public record DepartmentSummaryDto(Guid Id, string Name);

/// <summary>
/// Lightweight user security group identifier and name tuple.
/// </summary>
public record UserGroupSummaryDto(Guid Id, string Name);

