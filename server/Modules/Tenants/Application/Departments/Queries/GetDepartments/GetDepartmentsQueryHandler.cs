using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Departments.DTOs;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Queries;

/// <summary>
/// Handles the <see cref="GetDepartmentsQuery"/> by querying the database for the current tenant's departments.
/// </summary>
public class GetDepartmentsQueryHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<GetDepartmentsQuery, List<DepartmentDto>>
{
    public async Task<List<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Ensures that the caller has selected an active workspace before requesting department rosters.
        if (!tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);
        }

        // Multi-Tenant Isolation & Projection:
        // Returns all departments matching the active TenantId ordered alphabetically.
        // DTO projection isolates internal schema properties (e.g. audit timestamps) from public consumption.
        return await dbContext.Departments
            .AsNoTracking()
            .Where(d => d.TenantId == tenantContext.TenantId.Value)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name))
            .ToListAsync(cancellationToken);
    }
}

