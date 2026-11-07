using MediatR;
using Normora.Modules.Tenants.Application.Dashboard.DTOs;

namespace Normora.Modules.Tenants.Application.Dashboard.Queries;

/// <summary>
/// Aggregates organization-wide metrics and trend indicators for the employer analytics dashboard.
/// Handled by <see cref="GetDashboardSummaryQueryHandler"/>.
/// </summary>
/// <remarks>
/// Cross-Module Composition Architecture:
/// - Instead of directly querying foreign module tables, this query dispatches MediatR contract queries:
///   * <see cref="Normora.Shared.Contracts.Documents.GetDocumentsDashboardMetricsQuery"/> (knowledge base volume, coverage)
///   * <see cref="Normora.Shared.Contracts.Conversations.GetConversationsDashboardMetricsQuery"/> (chat counts, quality ratings)
/// - Assembles cross-system analytics safely respecting modular monolith domain isolation.
/// </remarks>
public record GetDashboardSummaryQuery() : IRequest<DashboardSummaryDto>;

