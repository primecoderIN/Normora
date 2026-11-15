using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.UpdateTenantBillingDetails;

public record UpdateTenantBillingDetailsCommand(
    string BillingEmail,
    string CompanyLegalName,
    string TaxId,
    string BillingAddress
) : IRequest<TenantBillingDetailsDto>;

