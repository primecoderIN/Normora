using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;

namespace Normora.Modules.Tenants.Application.Subscription.Queries;

/// <summary>
/// Query to aggregate real-time resource utilization against subscription tier limits for the ambient tenant.
/// Handled by <see cref="GetTenantSubscriptionUsageQueryHandler"/>.
/// </summary>
/// <remarks>
/// Cross-Module Aggregation:
/// - Fetches active <see cref="Domain.TenantPlan"/> from Tenants persistence.
/// - Queries active document count and aggregate storage size from Documents module via MediatR contract.
/// - Evaluates boolean threshold flags (IsDocumentLimitReached, IsStorageLimitReached) to enforce quota gates.
/// </remarks>
public record GetTenantSubscriptionUsageQuery() : IRequest<SubscriptionUsageDto>;

