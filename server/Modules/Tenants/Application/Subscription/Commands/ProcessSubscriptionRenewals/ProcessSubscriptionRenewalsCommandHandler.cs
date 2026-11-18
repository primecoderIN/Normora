using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.ProcessSubscriptionRenewals;

/// <summary>
/// Executes scheduled automated renewal debits for expiring subscriptions with active mandates.
/// </summary>
public class ProcessSubscriptionRenewalsCommandHandler(
    TenantsDbContext context,
    IPaymentService paymentService,
    ILogger<ProcessSubscriptionRenewalsCommandHandler> logger) : IRequestHandler<ProcessSubscriptionRenewalsCommand, SubscriptionRenewalsResultDto>
{
    public async Task<SubscriptionRenewalsResultDto> Handle(ProcessSubscriptionRenewalsCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Query active paid subscription plans due for auto-debit renewal
        var expiringPlans = await context.TenantPlans
            .Where(p => p.AutoRenewEnabled &&
                        p.SubscriptionStatus == "active" &&
                        !string.IsNullOrWhiteSpace(p.MandateToken) &&
                        p.CurrentPeriodEndUtc != null &&
                        p.CurrentPeriodEndUtc <= now &&
                        p.PlanTier != PlanTier.Free)
            .ToListAsync(cancellationToken);

        if (expiringPlans.Count == 0)
        {
            logger.LogDebug("No subscription renewals due at this time ({Now:u}).", now);
            return new SubscriptionRenewalsResultDto(0, 0, 0);
        }

        logger.LogInformation("Processing {Count} recurring subscription renewals.", expiringPlans.Count);

        int successfulCount = 0;
        int failedCount = 0;

        foreach (var plan in expiringPlans)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var tenant = await context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == plan.TenantId, cancellationToken);

                var billingDetails = await context.TenantBillingDetails
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.TenantId == plan.TenantId, cancellationToken);

                var contactEmail = billingDetails?.BillingEmail ?? "billing@normora.internal";
                var contactName = billingDetails?.CompanyLegalName ?? tenant?.Name ?? "Workspace";

                var currency = TenantPlanPricingCatalog.NormalizeCurrency(plan.Currency);
                var amount = TenantPlanPricingCatalog.GetPrice(plan.PlanTier, currency, "monthly");

                var result = await paymentService.ExecuteRecurringPaymentAsync(
                    plan.TenantId,
                    amount,
                    plan.MandateToken!,
                    plan.PlanTier,
                    currency,
                    contactEmail,
                    contactName,
                    cancellationToken);

                if (result.Success)
                {
                    successfulCount++;
                    plan.CurrentPeriodEndUtc = (plan.CurrentPeriodEndUtc ?? now).AddMonths(1);
                    plan.UpdatedAt = now;

                    // Record itemized paid invoice
                    context.TenantInvoices.Add(new TenantInvoice
                    {
                        TenantId = plan.TenantId,
                        InvoiceNumber = $"INV-{now:yyyyMM}-{result.TransactionId[..Math.Min(result.TransactionId.Length, 8)]}",
                        InvoiceDate = now,
                        PlanTier = plan.PlanTier.ToString(),
                        Amount = result.Amount,
                        Currency = currency.ToUpperInvariant(),
                        BillingCycle = "Monthly",
                        Status = "Paid",
                        PaymentTransactionId = result.TransactionId,
                        CustomerName = contactName,
                        CustomerEmail = contactEmail
                    });

                    // Record audit trail
                    context.AuditLogs.Add(new AuditLog
                    {
                        TenantId = plan.TenantId,
                        Action = "Subscription.AutoRenewCompleted",
                        TargetType = "TenantPlan",
                        TargetId = plan.Id.ToString(),
                        ActorUserId = "normora_worker",
                        ActorEmail = contactEmail,
                        ActorName = "Normora Billing Engine",
                        Metadata = JsonSerializer.Serialize(new
                        {
                            tier = plan.PlanTier.ToString(),
                            txnid = result.TransactionId,
                            amount = result.Amount,
                            currency
                        })
                    });

                    logger.LogInformation("Successfully auto-renewed subscription for Tenant {TenantId}, Tier {Tier}, TxnId {TxnId}",
                        plan.TenantId, plan.PlanTier, result.TransactionId);
                }
                else
                {
                    failedCount++;
                    plan.SubscriptionStatus = "past_due";
                    plan.UpdatedAt = now;

                    context.AuditLogs.Add(new AuditLog
                    {
                        TenantId = plan.TenantId,
                        Action = "Subscription.AutoRenewFailed",
                        TargetType = "TenantPlan",
                        TargetId = plan.Id.ToString(),
                        ActorUserId = "normora_worker",
                        ActorEmail = contactEmail,
                        ActorName = "Normora Billing Engine",
                        Metadata = JsonSerializer.Serialize(new
                        {
                            tier = plan.PlanTier.ToString(),
                            error = result.ErrorMessage
                        })
                    });

                    logger.LogWarning("Failed auto-renewal debit for Tenant {TenantId}: {Error}",
                        plan.TenantId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogError(ex, "Unhandled exception during recurring renewal for Tenant {TenantId}", plan.TenantId);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return new SubscriptionRenewalsResultDto(expiringPlans.Count, successfulCount, failedCount);
    }
}
