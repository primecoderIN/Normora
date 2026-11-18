using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Application.Subscription.Queries;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.CancelSubscription;

/// <summary>
/// Handles <see cref="CancelSubscriptionCommand"/> by scheduling cancellation at period end or reverting to Free tier.
/// </summary>
public class CancelSubscriptionCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<CancelSubscriptionCommand, SubscriptionUsageDto>
{
    public async Task<SubscriptionUsageDto> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Validates that subscription cancellation operates on the current tenant workspace.
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        // A workspace on the Free tier has no active paid subscription to cancel
        if (plan == null || plan.PlanTier == PlanTier.Free)
        {
            throw new InvalidOperationException(ApiMessages.CannotCancelFreePlan);
        }

        // Mark cancellation flag, disable auto-renew, and set subscription status
        plan.CancelAtPeriodEnd = true;
        plan.AutoRenewEnabled = false;
        plan.SubscriptionStatus = "canceled";
        plan.UpdatedAt = DateTime.UtcNow;

        if (request.Immediate)
        {
            var freeTemplate = TenantPlan.CreateDefault(tenantId, PlanTier.Free, plan.Currency);
            plan.PlanTier = PlanTier.Free;
            plan.MaxDocuments = freeTemplate.MaxDocuments;
            plan.MaxEmployees = freeTemplate.MaxEmployees;
            plan.MaxStorageBytes = freeTemplate.MaxStorageBytes;
            plan.SubscriptionStatus = "free";
            plan.CancelAtPeriodEnd = false;
            plan.CurrentPeriodEndUtc = null;
            plan.PaymentTransactionId = null;
            plan.PaymentReferenceId = null;
            plan.MandateToken = null;
            plan.AutoRenewEnabled = false;
        }

        await context.SaveChangesAsync(cancellationToken);

        // Return updated usage metrics with cancellation state
        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}
