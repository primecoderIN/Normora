using MediatR;
using Normora.Shared.Interfaces;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Paginated query to retrieve tenant employees with department and group filters.
/// </summary>
public record GetTenantEmployeesQuery(
    Guid TenantId, 
    int Page, 
    int PageSize, 
    string? Search, 
    Guid? DepartmentId, 
    Guid? UserGroupId) : IRequest<PagedResult<TenantEmployeeDto>>;

/// <summary>
/// Enriched employee profile containing membership ID, user ID, role, and department/group associations.
/// </summary>
public record TenantEmployeeDto(
    Guid MembershipId,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    DateTime JoinedAt,
    IReadOnlyList<DepartmentSummaryDto> Departments,
    IReadOnlyList<UserGroupSummaryDto> UserGroups
);

/// <summary>Brief summary of an employee's department.</summary>
public record DepartmentSummaryDto(Guid Id, string Name);

/// <summary>Brief summary of an employee's user group.</summary>
public record UserGroupSummaryDto(Guid Id, string Name);


