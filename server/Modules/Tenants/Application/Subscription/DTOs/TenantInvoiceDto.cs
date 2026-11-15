namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

public record TenantInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    DateTime Date,
    string Tier,
    string Amount,
    string Currency,
    string Cycle,
    string Status,
    string? TransactionId
);

