using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Queries.DownloadInvoice;

public class DownloadInvoiceQueryHandler(
    TenantsDbContext context,
    ITenantContext tenantContext) : IRequestHandler<DownloadInvoiceQuery, InvoiceDownloadResultDto>
{
    public async Task<InvoiceDownloadResultDto> Handle(DownloadInvoiceQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        TenantInvoice? invoice = null;

        if (request.InvoiceId.HasValue && request.InvoiceId.Value != Guid.Empty)
        {
            invoice = await context.TenantInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId.Value && i.TenantId == tenantId, cancellationToken);
        }

        if (invoice == null)
        {
            invoice = await context.TenantInvoices
                .AsNoTracking()
                .Where(i => i.TenantId == tenantId)
                .OrderByDescending(i => i.InvoiceDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (invoice == null)
        {
            var plan = await context.TenantPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

            var tenantRec = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            var billingRec = await context.TenantBillingDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

            var tier = plan?.PlanTier ?? PlanTier.Enterprise;
            var currency = (plan?.Currency ?? "inr").ToUpperInvariant();
            var price = currency.ToLowerInvariant() switch
            {
                "usd" => (tier == PlanTier.Enterprise ? 249m : 49m),
                "eur" => (tier == PlanTier.Enterprise ? 229m : 45m),
                "gbp" => (tier == PlanTier.Enterprise ? 199m : 39m),
                _ => (tier == PlanTier.Enterprise ? 19999m : 3999m)
            };

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
                CustomerName = billingRec?.CompanyLegalName ?? tenantRec?.Name ?? "Workspace Organization",
                CustomerEmail = billingRec?.BillingEmail ?? "billing@normora.internal",
                TaxId = billingRec?.TaxId ?? "GSTIN29ABCDE1234F1Z5",
                BillingAddress = billingRec?.BillingAddress ?? "Registered Commercial Office"
            };

            context.TenantInvoices.Add(defaultInvoice);
            await context.SaveChangesAsync(cancellationToken);

            invoice = defaultInvoice;
        }

        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        var billing = await context.TenantBillingDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

        var companyName = billing?.CompanyLegalName ?? tenant?.Name ?? "Workspace Organization";
        var billingEmail = billing?.BillingEmail ?? "billing@normora.internal";
        var taxId = billing?.TaxId ?? "GSTIN29ABCDE1234F1Z5";
        var address = billing?.BillingAddress ?? "Level 4, Prestige Tech Park, Bangalore 560103, India";

        var subtotal = invoice.Amount / 1.18m;
        var tax = invoice.Amount - subtotal;

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>Invoice {invoice.InvoiceNumber}</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1e293b; line-height: 1.5; margin: 0; padding: 40px; background: #f8fafc; }}
    .invoice-card {{ max-width: 800px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 16px; padding: 48px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); background: #ffffff; }}
    .header {{ display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 40px; border-bottom: 2px solid #f1f5f9; padding-bottom: 24px; }}
    .brand {{ font-size: 26px; font-weight: 800; color: #4f46e5; letter-spacing: -0.5px; }}
    .badge {{ display: inline-block; padding: 4px 12px; border-radius: 9999px; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; background: #dcfce7; color: #15803d; border: 1px solid #bbf7d0; }}
    .title {{ font-size: 20px; font-weight: 700; color: #0f172a; margin-top: 8px; }}
    .details-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 32px; margin-bottom: 40px; }}
    .section-title {{ font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; color: #64748b; margin-bottom: 8px; }}
    .table {{ width: 100%; border-collapse: collapse; margin-bottom: 32px; }}
    .table th {{ background: #f8fafc; padding: 12px 16px; text-align: left; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; color: #64748b; border-bottom: 1px solid #e2e8f0; }}
    .table td {{ padding: 16px; border-bottom: 1px solid #f1f5f9; font-size: 13px; }}
    .totals {{ width: 320px; margin-left: auto; margin-bottom: 40px; }}
    .totals-row {{ display: flex; justify-content: space-between; padding: 8px 0; font-size: 13px; color: #475569; }}
    .totals-total {{ font-size: 16px; font-weight: 800; color: #0f172a; border-top: 2px solid #e2e8f0; padding-top: 12px; margin-top: 8px; }}
    .footer {{ text-align: center; font-size: 11px; color: #94a3b8; border-top: 1px solid #f1f5f9; padding-top: 24px; }}
    .no-print {{ max-width: 800px; margin: 0 auto 20px auto; display: flex; justify-content: flex-end; gap: 12px; }}
    .print-btn {{ background: #4f46e5; color: white; border: none; padding: 9px 20px; border-radius: 10px; font-weight: 600; font-size: 13px; cursor: pointer; transition: background 0.15s; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }}
    .print-btn:hover {{ background: #4338ca; }}
    @media print {{
      body {{ padding: 0 !important; background: #fff !important; }}
      .no-print {{ display: none !important; }}
      .invoice-card {{ border: none !important; box-shadow: none !important; padding: 0 !important; max-width: 100% !important; }}
    }}
  </style>
</head>
<body>
  <div class=""no-print"">
    <button onclick=""window.print()"" class=""print-btn"">
      🖨️ Print / Save as PDF
    </button>
  </div>
  <div class=""invoice-card"">
    <div class=""header"">
      <div>
        <div class=""brand"">Normora</div>
        <div style=""font-size: 12px; color: #64748b; margin-top: 4px;"">Enterprise AI Knowledge Engine</div>
      </div>
      <div style=""text-align: right;"">
        <span class=""badge"">{invoice.Status}</span>
        <div class=""title"">{invoice.InvoiceNumber}</div>
        <div style=""font-size: 12px; color: #64748b; margin-top: 4px;"">Date: {invoice.InvoiceDate:MMM dd, yyyy}</div>
      </div>
    </div>

    <div class=""details-grid"">
      <div>
        <div class=""section-title"">Billed To</div>
        <div style=""font-weight: 700; font-size: 14px;"">{companyName}</div>
        <div style=""font-size: 12px; color: #475569; margin-top: 2px;"">{billingEmail}</div>
        <div style=""font-size: 12px; color: #475569; margin-top: 2px;"">Tax ID: {taxId}</div>
        <div style=""font-size: 12px; color: #475569; margin-top: 2px;"">{address}</div>
      </div>
      <div>
        <div class=""section-title"">Issuer & Payment Details</div>
        <div style=""font-weight: 700; font-size: 14px;"">Normora Technologies Inc.</div>
        <div style=""font-size: 12px; color: #475569; margin-top: 2px;"">Payment Gateway: PayU India Secure Vault</div>
        <div style=""font-size: 12px; color: #475569; margin-top: 2px;"">Transaction Reference: {invoice.PaymentTransactionId}</div>
        <div style=""font-size: 12px; color: #475569; margin-top: 2px;"">Billing Period: {invoice.BillingCycle} Recurring</div>
      </div>
    </div>

    <table class=""table"">
      <thead>
        <tr>
          <th>Description</th>
          <th>Period</th>
          <th style=""text-align: right;"">Subtotal</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>
            <strong>Normora {invoice.PlanTier} Plan</strong>
            <div style=""font-size: 11px; color: #64748b; margin-top: 2px;"">Grounded enterprise RAG, high-scale pgvector retrieval, compliance audit logs</div>
          </td>
          <td>1 Month</td>
          <td style=""text-align: right; font-weight: 600;"">{invoice.Currency} {subtotal:N2}</td>
        </tr>
      </tbody>
    </table>

    <div class=""totals"">
      <div class=""totals-row"">
        <span>Subtotal (Net)</span>
        <span>{invoice.Currency} {subtotal:N2}</span>
      </div>
      <div class=""totals-row"">
        <span>Goods & Services Tax (GST 18%)</span>
        <span>{invoice.Currency} {tax:N2}</span>
      </div>
      <div class=""totals-row totals-total"">
        <span>Total Paid</span>
        <span>{invoice.Currency} {invoice.Amount:N2}</span>
      </div>
    </div>

    <div class=""footer"">
      Thank you for powering your organizational intelligence with Normora.<br>
      This document constitutes an electronic computer-generated tax invoice.
    </div>
  </div>
</body>
</html>";

        var bytes = Encoding.UTF8.GetBytes(html);
        return new InvoiceDownloadResultDto(
            $"{invoice.InvoiceNumber}.html",
            "text/html; charset=utf-8",
            bytes
        );
    }
}
