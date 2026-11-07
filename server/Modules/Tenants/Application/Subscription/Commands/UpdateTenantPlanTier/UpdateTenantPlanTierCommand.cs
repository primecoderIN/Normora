using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

/// <summary>
/// Command to modify the subscription plan tier directly for an active tenant (e.g. testing, admin override, downgrade).
/// Handled by <see cref="UpdateTenantPlanTierCommandHandler"/>.
/// </summary>
/// <remarks>
/// Quota Synchronization:
/// - Reconfigures seat limits, document upload limits, and maximum storage byte quotas based on PlanLimits.
/// - Immediately returns the recalculated <see cref="SubscriptionUsageDto"/> to refresh frontend billing UI.
/// - Implements <see cref="IAuditableRequest"/> to record plan changes in the audit trail.
/// </remarks>
/// <param name="Tier">The new subscription tier to assign to the tenant.</param>
public record UpdateTenantPlanTierCommand(PlanTier Tier) : IRequest<SubscriptionUsageDto>, IAuditableRequest
{
    public string Action => "Subscription.TierUpdated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { tier = Tier.ToString() });
}

