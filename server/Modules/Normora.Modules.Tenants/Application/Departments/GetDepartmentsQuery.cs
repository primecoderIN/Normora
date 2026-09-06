using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments;

public record GetDepartmentsQuery : IRequest<List<DepartmentDto>>;

public record DepartmentDto(Guid Id, string Name);

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
