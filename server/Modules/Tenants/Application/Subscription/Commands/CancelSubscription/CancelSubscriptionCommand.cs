using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.CancelSubscription;

/// <summary>
/// Command to cancel the active subscription for the current tenant workspace.
/// Handled by <see cref="CancelSubscriptionCommandHandler"/>.
/// </summary>
/// <remarks>
/// Cancellation Lifecycle:
/// - Sets CancelAtPeriodEnd = true and SubscriptionStatus = "canceled" so paid features remain accessible until renewal date.
/// - When Immediate is true, immediately downgrades to PlanTier.Free and reverts quotas to Starter defaults.
/// - Implements <see cref="IAuditableRequest"/> for workspace audit logs.
/// </remarks>
/// <param name="Immediate">Whether to immediately revert quotas to Free tier or retain benefits until period end.</param>
public record CancelSubscriptionCommand(bool Immediate = false) : IRequest<SubscriptionUsageDto>, IAuditableRequest
{
    public string Action => "Subscription.Cancelled";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { immediate = Immediate });
}
