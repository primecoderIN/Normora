namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

public record TenantBillingDetailsDto(
    string BillingEmail,
    string CompanyLegalName,
    string TaxId,
    string BillingAddress
);

public record UpdateTenantBillingDetailsRequest(
    string BillingEmail,
    string CompanyLegalName,
    string TaxId,
    string BillingAddress
);

