using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;

namespace Normora.Modules.Tenants.Application.Subscription.Queries.GetTenantBillingDetails;

public record GetTenantBillingDetailsQuery : IRequest<TenantBillingDetailsDto>;

