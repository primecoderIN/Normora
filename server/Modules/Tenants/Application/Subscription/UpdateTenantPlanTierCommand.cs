using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription;

/// <summary>Changes the active tenant's plan tier and returns its updated quota usage.</summary>
public record UpdateTenantPlanTierCommand(PlanTier Tier) : IRequest<SubscriptionUsageDto>, IAuditableRequest
{
    public string Action => "Subscription.TierUpdated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { tier = Tier.ToString() });
}

public class UpdateTenantPlanTierCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<UpdateTenantPlanTierCommand, SubscriptionUsageDto>
{
    public async Task<SubscriptionUsageDto> Handle(UpdateTenantPlanTierCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve active tenant from ambient context
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        // 2. Fetch existing plan or initialize a new default record
        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (plan == null)
        {
            // Create initial plan entity with requested tier template quotas
            plan = TenantPlan.CreateDefault(tenantId, request.Tier);
            context.TenantPlans.Add(plan);
        }
        else
        {
            // Reassign tier quotas (documents, employees, storage bytes) based on plan tier template
            var template = TenantPlan.CreateDefault(tenantId, request.Tier);
            plan.PlanTier = request.Tier;
            plan.MaxDocuments = template.MaxDocuments;
            plan.MaxEmployees = template.MaxEmployees;
            plan.MaxStorageBytes = template.MaxStorageBytes;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        // 3. Return updated subscription usage dto
        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}
