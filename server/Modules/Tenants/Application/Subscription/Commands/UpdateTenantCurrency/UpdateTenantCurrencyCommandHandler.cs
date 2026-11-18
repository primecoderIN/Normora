using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Application.Subscription.Queries;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.UpdateTenantCurrency;

/// <summary>
/// Handles <see cref="UpdateTenantCurrencyCommand"/> by updating the tenant workspace's default currency preference.
/// BFLA guarded at the API controller layer (TenantRoles.Admin).
/// </summary>
public class UpdateTenantCurrencyCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<UpdateTenantCurrencyCommand, SubscriptionUsageDto>
{
    public async Task<SubscriptionUsageDto> Handle(UpdateTenantCurrencyCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var normalized = TenantPlanPricingCatalog.NormalizeCurrency(request.Currency);

        if (plan == null)
        {
            plan = TenantPlan.CreateDefault(tenantId, PlanTier.Free, normalized);
            context.TenantPlans.Add(plan);
        }
        else
        {
            plan.Currency = normalized;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}
