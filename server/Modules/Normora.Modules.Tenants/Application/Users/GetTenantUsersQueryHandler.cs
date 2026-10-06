using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Users;

public class GetTenantEmployeesQueryHandler(TenantsDbContext context) : IRequestHandler<GetTenantEmployeesQuery, PagedResult<TenantEmployeeDto>>
{
    public async Task<PagedResult<TenantEmployeeDto>> Handle(GetTenantEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = context.TenantMemberships
            .Include(m => m.User)
            .Include(m => m.MembershipDepartments).ThenInclude(md => md.Department)
            .Include(m => m.UserGroupMemberships).ThenInclude(ugm => ugm.UserGroup)
            .Where(m => m.TenantId == request.TenantId) // Soft-delete filter is applied automatically
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(m => 
                (m.User.DisplayName != null && m.User.DisplayName.ToLower().Contains(search)) || 
                (m.User.Email != null && m.User.Email.ToLower().Contains(search)));
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(m => m.MembershipDepartments.Any(md => md.DepartmentId == request.DepartmentId.Value));
        }

        if (request.UserGroupId.HasValue)
        {
            query = query.Where(m => m.UserGroupMemberships.Any(ugm => ugm.UserGroupId == request.UserGroupId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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
            request.Page,
            request.PageSize,
            totalPages
        );
    }
}
