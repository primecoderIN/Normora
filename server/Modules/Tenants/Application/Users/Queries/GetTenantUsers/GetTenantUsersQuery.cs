using MediatR;
using Normora.Modules.Tenants.Application.Users.DTOs;
using Normora.Shared.Interfaces;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Paginated query to retrieve tenant employees with department, user group, and free-text search filters.
/// Handled by <see cref="GetTenantUsersQueryHandler"/>.
/// </summary>
/// <remarks>
/// Directory Query Invariants:
/// - Verifies that the requested TenantId matches the ambient tenant (BOLA isolation).
/// - Excludes soft-deleted employees (RemovedAt != null) via global query filters.
/// - Performs efficient EF Core subqueries to project assigned departments and user groups without N+1 query overhead.
/// </remarks>
/// <param name="TenantId">Target organization identifier.</param>
/// <param name="Page">1-based page index.</param>
/// <param name="PageSize">Page size window (clamped to max 100).</param>
/// <param name="Search">Optional substring filter matching email or display name.</param>
/// <param name="DepartmentId">Optional department filter.</param>
/// <param name="UserGroupId">Optional user security group filter.</param>
public record GetTenantEmployeesQuery(
    Guid TenantId, 
    int Page, 
    int PageSize, 
    string? Search, 
    Guid? DepartmentId, 
    Guid? UserGroupId
) : IRequest<PagedResult<TenantEmployeeDto>>;



