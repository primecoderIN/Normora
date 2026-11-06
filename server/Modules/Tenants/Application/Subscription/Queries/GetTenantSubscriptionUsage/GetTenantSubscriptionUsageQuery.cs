using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;

namespace Normora.Modules.Tenants.Application.Subscription.Queries;

public record GetTenantSubscriptionUsageQuery() : IRequest<SubscriptionUsageDto>;

