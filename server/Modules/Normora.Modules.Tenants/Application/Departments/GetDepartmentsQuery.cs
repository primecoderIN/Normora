using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments;

/// <summary>
/// Query to retrieve a list of all departments for the current tenant.
/// </summary>
public record GetDepartmentsQuery : IRequest<List<DepartmentDto>>;

/// <summary>
/// Data transfer object representing a department.
/// </summary>
public record DepartmentDto(Guid Id, string Name);

/// <summary>
/// Handles the <see cref="GetDepartmentsQuery"/> by querying the database for the current tenant's departments.
/// </summary>
public class GetDepartmentsQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetDepartmentsQuery, List<DepartmentDto>>
{
    public async Task<List<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant Context missing.");
        }

        return await dbContext.Departments
            .Where(d => d.TenantId == tenantContext.TenantId.Value)
            .Select(d => new DepartmentDto(d.Id, d.Name))
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }
}
