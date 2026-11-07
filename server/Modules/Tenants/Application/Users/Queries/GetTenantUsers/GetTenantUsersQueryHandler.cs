using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Handles fetching and filtering tenant employees with department and group joins.
/// </summary>
public class GetTenantEmployeesQueryHandler(TenantsDbContext context) : IRequestHandler<GetTenantEmployeesQuery, PagedResult<TenantEmployeeDto>>
{
    public async Task<PagedResult<TenantEmployeeDto>> Handle(GetTenantEmployeesQuery request, CancellationToken cancellationToken)
    {
        // Tenant Boundary & Soft-Delete Enforcement:
        // Query active memberships scoped to request.TenantId.
        // Members marked as soft-deleted (RemovedAt != null) are automatically filtered out by EF Core global query filters.
        var query = context.TenantMemberships
            .Where(m => m.TenantId == request.TenantId)
            .AsNoTracking();

        // Case-Insensitive Multi-Field Search:
        // Uses PostgreSQL ILike via EF.Functions for index-accelerated pattern matching across DisplayName and Email.
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchPattern = $"%{request.Search.Trim()}%";
            query = query.Where(m => 
                (m.User.DisplayName != null && EF.Functions.ILike(m.User.DisplayName, searchPattern)) || 
                (m.User.Email != null && EF.Functions.ILike(m.User.Email, searchPattern)));
        }

        // Relational Department Filter:
        // Restricts results to employees associated with a specific department through MembershipDepartments.
        if (request.DepartmentId.HasValue)
        {
            query = query.Where(m => m.MembershipDepartments.Any(md => md.DepartmentId == request.DepartmentId.Value));
        }

        // Relational User Group Filter:
        // Restricts results to employees belonging to a specific user group via UserGroupMemberships.
        if (request.UserGroupId.HasValue)
        {
            query = query.Where(m => m.UserGroupMemberships.Any(ugm => ugm.UserGroupId == request.UserGroupId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Resource Exhaustion Defense (OWASP API4):
        // Clamps page and pageSize to deterministic bounds (1 to 100), neutralizing unbounded DB allocations
        // and preventing division by zero or negative skip offsets.
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Keyset-Friendly Pagination with Relational Projections:
        // Orders by CreatedAt descending to show newest members first.
        // Nested selects project only the required department and group summaries, avoiding Cartesian row explosion.
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new TenantEmployeeDto(
                m.Id,
                m.UserId,
                m.User.Email ?? string.Empty,
                m.User.DisplayName ?? string.Empty,
                m.Role.ToString(),
                m.CreatedAt,
                m.MembershipDepartments.Select(md => new DepartmentSummaryDto(md.Department.Id, md.Department.Name)).ToList(),
                m.UserGroupMemberships.Select(ugm => new UserGroupSummaryDto(ugm.UserGroup.Id, ugm.UserGroup.Name)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantEmployeeDto>(
            items,
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}

