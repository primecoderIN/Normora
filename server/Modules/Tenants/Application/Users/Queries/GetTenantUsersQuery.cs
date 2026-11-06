using MediatR;
using Normora.Modules.Tenants.Application.Users.DTOs;
using Normora.Shared.Interfaces;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Users.Queries;

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



