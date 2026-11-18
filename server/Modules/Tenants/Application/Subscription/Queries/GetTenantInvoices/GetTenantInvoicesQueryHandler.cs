using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Queries.GetTenantInvoices;

public class GetTenantInvoicesQueryHandler(
    TenantsDbContext context,
    ITenantContext tenantContext) : IRequestHandler<GetTenantInvoicesQuery, List<TenantInvoiceDto>>
{
    public async Task<List<TenantInvoiceDto>> Handle(GetTenantInvoicesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var invoices = await context.TenantInvoices
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        // If no records in database yet and tenant has an active paid plan, synthesize from TenantPlan
        if (invoices.Count == 0)
        {
            var plan = await context.TenantPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

            if (plan != null && plan.PlanTier != PlanTier.Free && !string.IsNullOrWhiteSpace(plan.PaymentTransactionId))
            {
                var tenant = await context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

                var billing = await context.TenantBillingDetails
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

                var tier = plan.PlanTier;
                var currency = TenantPlanPricingCatalog.NormalizeCurrency(plan.Currency);
                var price = TenantPlanPricingCatalog.GetPrice(tier, currency, "monthly");

                var defaultInvoice = new TenantInvoice
                {
                    TenantId = tenantId,
                    InvoiceNumber = $"INV-{DateTime.UtcNow:yyyy-MM}-001",
                    InvoiceDate = plan.CreatedAt,
                    PlanTier = tier.ToString(),
                    Amount = price,
                    Currency = currency.ToUpperInvariant(),
                    BillingCycle = "Monthly",
                    Status = "Paid",
                    PaymentTransactionId = plan.PaymentTransactionId,
                    CustomerName = billing?.CompanyLegalName ?? tenant?.Name ?? "Workspace Organization",
                    CustomerEmail = billing?.BillingEmail ?? "billing@normora.internal",
                    TaxId = billing?.TaxId ?? "GSTIN29ABCDE1234F1Z5",
                    BillingAddress = billing?.BillingAddress ?? "Registered Commercial Office"
                };

                context.TenantInvoices.Add(defaultInvoice);
                await context.SaveChangesAsync(cancellationToken);

                invoices = new List<TenantInvoice> { defaultInvoice };
            }
        }

        return invoices.Select(i => new TenantInvoiceDto(
            i.Id,
            i.InvoiceNumber,
            i.InvoiceDate,
            i.PlanTier == "Pro" ? "Pro Workspace" : i.PlanTier,
            TenantPlanPricingCatalog.FormatPrice(i.Amount, i.Currency),
            i.Currency,
            i.BillingCycle,
            i.Status,
            i.PaymentTransactionId
        )).ToList();
    }
}

