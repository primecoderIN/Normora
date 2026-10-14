using MediatR;
using Normora.Shared.Interfaces;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Users;

public record GetTenantEmployeesQuery(
    Guid TenantId, 
    int Page, 
    int PageSize, 
    string? Search, 
    Guid? DepartmentId, 
    Guid? UserGroupId) : IRequest<PagedResult<TenantEmployeeDto>>;

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

public record DepartmentSummaryDto(Guid Id, string Name);
public record UserGroupSummaryDto(Guid Id, string Name);

