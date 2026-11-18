using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Application.Subscription.Queries;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.ToggleAutoRenew;

/// <summary>
/// Handles toggling automated recurring renewals for the active tenant's subscription.
/// </summary>
public class ToggleAutoRenewCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<ToggleAutoRenewCommand, SubscriptionUsageDto>
{
    public async Task<SubscriptionUsageDto> Handle(ToggleAutoRenewCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (plan == null)
        {
            plan = TenantPlan.CreateDefault(tenantId);
            context.TenantPlans.Add(plan);
        }

        // Cannot enable auto-renewal without an authorized mandate token
        if (request.Enabled && string.IsNullOrWhiteSpace(plan.MandateToken))
        {
            throw new InvalidOperationException(ApiMessages.AutoRenewNotAvailable);
        }

        plan.AutoRenewEnabled = request.Enabled;
        plan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}
