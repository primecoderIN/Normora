using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Encapsulates employee metrics and department lookups for the tenant dashboard.
/// </summary>
public sealed record TenantDashboardMetricsResult(
    int TotalEmployees,
    int EmployeesTrend,
    IReadOnlyDictionary<Guid, string> DepartmentNames);

/// <summary>
/// Cross-module query to retrieve employee metrics and resolve department names without leaking TenantsDbContext.
/// </summary>
public sealed record GetTenantDashboardMetricsQuery(
    Guid TenantId,
    IReadOnlyList<Guid> DepartmentIds) : IRequest<TenantDashboardMetricsResult>;
