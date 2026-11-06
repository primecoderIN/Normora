using MediatR;
using Normora.Modules.Tenants.Application.Dashboard.DTOs;

namespace Normora.Modules.Tenants.Application.Dashboard.Queries;

/// <summary>Aggregates dashboard metrics through contracts owned by the respective modules.</summary>
public record GetDashboardSummaryQuery() : IRequest<DashboardSummaryDto>;

