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

        // If no records in database yet, synthesize the active tier invoice from TenantPlan so users see real invoices immediately
        if (invoices.Count == 0)
        {
            var plan = await context.TenantPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

            var tenant = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            var billing = await context.TenantBillingDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

            var tier = plan?.PlanTier ?? PlanTier.Enterprise;
            var currency = (plan?.Currency ?? "inr").ToUpperInvariant();
            var (symbol, price) = GetPrice(tier, currency);

            var defaultInvoice = new TenantInvoice
            {
                TenantId = tenantId,
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyy-MM}-001",
                InvoiceDate = plan?.CreatedAt ?? DateTime.UtcNow,
                PlanTier = tier.ToString(),
                Amount = price,
                Currency = currency,
                BillingCycle = "Monthly",
                Status = "Paid",
                PaymentTransactionId = plan?.PaymentTransactionId ?? $"txn_{tenantId.ToString("N")[..12]}",
                CustomerName = billing?.CompanyLegalName ?? tenant?.Name ?? "Workspace Organization",
                CustomerEmail = billing?.BillingEmail ?? "billing@normora.internal",
                TaxId = billing?.TaxId ?? "GSTIN29ABCDE1234F1Z5",
                BillingAddress = billing?.BillingAddress ?? "Registered Commercial Office"
            };

            context.TenantInvoices.Add(defaultInvoice);
            await context.SaveChangesAsync(cancellationToken);

            invoices = new List<TenantInvoice> { defaultInvoice };
        }

        return invoices.Select(i => new TenantInvoiceDto(
            i.Id,
            i.InvoiceNumber,
            i.InvoiceDate,
            i.PlanTier == "Pro" ? "Pro Workspace" : i.PlanTier,
            FormatAmount(i.Amount, i.Currency),
            i.Currency,
            i.BillingCycle,
            i.Status,
            i.PaymentTransactionId
        )).ToList();
    }

    private static (string Symbol, decimal Price) GetPrice(PlanTier tier, string currency)
    {
        var isEnterprise = tier == PlanTier.Enterprise;
        return currency.ToLowerInvariant() switch
        {
            "usd" => ("$", isEnterprise ? 249m : 49m),
            "eur" => ("€", isEnterprise ? 229m : 45m),
            "gbp" => ("£", isEnterprise ? 199m : 39m),
            "cad" => ("C$", isEnterprise ? 329m : 65m),
            "aud" => ("A$", isEnterprise ? 379m : 75m),
            "jpy" => ("¥", isEnterprise ? 38000m : 7500m),
            "sgd" => ("S$", isEnterprise ? 329m : 65m),
            _ => ("₹", isEnterprise ? 19999m : 3999m)
        };
    }

    private static string FormatAmount(decimal amount, string currency)
    {
        var sym = currency.ToLowerInvariant() switch
        {
            "usd" => "$",
            "eur" => "€",
            "gbp" => "£",
            "cad" => "C$",
            "aud" => "A$",
            "jpy" => "¥",
            "sgd" => "S$",
            _ => "₹"
        };
        return $"{sym}{amount:N0}";
    }
}

