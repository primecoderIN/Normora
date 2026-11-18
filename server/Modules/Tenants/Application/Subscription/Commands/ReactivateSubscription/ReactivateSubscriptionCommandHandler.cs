using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Application.Subscription.Queries;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.ReactivateSubscription;

/// <summary>
/// Handles <see cref="ReactivateSubscriptionCommand"/> by resetting CancelAtPeriodEnd and keeping subscription active.
/// </summary>
public class ReactivateSubscriptionCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<ReactivateSubscriptionCommand, SubscriptionUsageDto>
{
    public async Task<SubscriptionUsageDto> Handle(ReactivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        // A workspace on the Free tier has no paid subscription to reactivate
        if (plan == null || plan.PlanTier == PlanTier.Free)
        {
            throw new InvalidOperationException(ApiMessages.CannotReactivateFreePlan);
        }

        plan.CancelAtPeriodEnd = false;
        plan.SubscriptionStatus = "active";
        if (!string.IsNullOrWhiteSpace(plan.MandateToken))
        {
            plan.AutoRenewEnabled = true;
        }
        plan.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}
