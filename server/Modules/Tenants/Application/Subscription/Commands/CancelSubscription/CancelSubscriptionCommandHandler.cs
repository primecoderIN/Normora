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

        if (plan == null)
        {
            plan = TenantPlan.CreateDefault(tenantId);
            context.TenantPlans.Add(plan);
        }

        // Mark cancellation flag and subscription status
        plan.CancelAtPeriodEnd = true;
        plan.SubscriptionStatus = "canceled";
        plan.UpdatedAt = DateTime.UtcNow;

        if (request.Immediate)
        {
            var freeTemplate = TenantPlan.CreateDefault(tenantId, PlanTier.Free);
            plan.PlanTier = PlanTier.Free;
            plan.MaxDocuments = freeTemplate.MaxDocuments;
            plan.MaxEmployees = freeTemplate.MaxEmployees;
            plan.MaxStorageBytes = freeTemplate.MaxStorageBytes;
        }

        await context.SaveChangesAsync(cancellationToken);

        // Return updated usage metrics with cancellation state
        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}
