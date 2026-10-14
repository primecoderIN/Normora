using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription;

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
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (plan == null)
        {
            plan = TenantPlan.CreateDefault(tenantId, request.Tier);
            context.TenantPlans.Add(plan);
        }
        else
        {
            var template = TenantPlan.CreateDefault(tenantId, request.Tier);
            plan.PlanTier = request.Tier;
            plan.MaxDocuments = template.MaxDocuments;
            plan.MaxEmployees = template.MaxEmployees;
            plan.MaxStorageBytes = template.MaxStorageBytes;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
    }
}
