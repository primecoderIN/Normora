using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Application.Subscription.Queries;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

/// <summary>
/// Handles <see cref="UpdateTenantPlanTierCommand"/> by updating the tenant's subscription tier and recalculating quotas.
/// Enforces Broken Function-Level Authorization (BFLA) guards: only direct downgrades to <see cref="PlanTier.Free"/>
/// are permitted without a verified payment gateway transaction; paid tiers must be acquired via checkout.
/// </summary>
public class UpdateTenantPlanTierCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<UpdateTenantPlanTierCommand, SubscriptionUsageDto>
{
    public async Task<SubscriptionUsageDto> Handle(UpdateTenantPlanTierCommand request, CancellationToken cancellationToken)
    {
        // Ambient Tenant Verification:
        // Validates that plan tier updates operate on the current tenant workspace.
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        // BFLA Guard: Only downgrading to the Free tier is permitted directly without a payment transaction.
        // Paid tiers (Pro, Enterprise) must be acquired through the verified checkout flow.
        if (request.Tier != PlanTier.Free)
        {
            throw new Normora.Shared.Exceptions.BflaException(ApiMessages.DirectTierUpgradeNotAllowed);
        }

        // Plan Record Upsert Workflow:
        // Locate the existing tenant subscription record, or initialize a new default record if first time.
        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (plan == null)
        {
            // Initial plan creation: seed default quota templates for the chosen tier (Starter/Pro/Enterprise)
            plan = TenantPlan.CreateDefault(tenantId, request.Tier);
            context.TenantPlans.Add(plan);
        }
        else
        {
            // Plan Tier Upgrade / Downgrade Invariants:
            // Recalculate tier quotas (MaxDocuments, MaxEmployees, MaxStorageBytes) from tier defaults.
            // Updates are immediate, and the updated timestamp is refreshed.
            var template = TenantPlan.CreateDefault(tenantId, request.Tier);
            plan.PlanTier = request.Tier;
            plan.MaxDocuments = template.MaxDocuments;
            plan.MaxEmployees = template.MaxEmployees;
            plan.MaxStorageBytes = template.MaxStorageBytes;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        // Cross-Module Usage Synchronization:
        // Re-query subscription usage via MediatR to return the new operational quotas and current usage metrics to the caller.
        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}

