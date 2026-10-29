using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Contracts.Tenants;

namespace Normora.Modules.Tenants.Application.Departments;

/// <summary>
/// Handles GetTenantDashboardMetricsQuery within the Tenants module boundary.
/// </summary>
public sealed class GetTenantDashboardMetricsQueryHandler(TenantsDbContext dbContext)
    : IRequestHandler<GetTenantDashboardMetricsQuery, TenantDashboardMetricsResult>
{
    public async Task<TenantDashboardMetricsResult> Handle(
        GetTenantDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var totalEmployees = await dbContext.TenantMemberships
            .CountAsync(cancellationToken);

        var employeesThisMonth = await dbContext.TenantMemberships
            .Where(m => m.CreatedAt >= thirtyDaysAgo)
            .CountAsync(cancellationToken);

        var departments = new Dictionary<Guid, string>();
        if (request.DepartmentIds.Count > 0)
        {
            departments = await dbContext.Departments
                .Where(d => request.DepartmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);
        }

        return new TenantDashboardMetricsResult(totalEmployees, employeesThisMonth, departments);
    }
}
