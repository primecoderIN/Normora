using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.ReactivateSubscription;

/// <summary>
/// Command to resume / reactivate a subscription previously marked for cancellation at period end.
/// Handled by <see cref="ReactivateSubscriptionCommandHandler"/>.
/// </summary>
public record ReactivateSubscriptionCommand : IRequest<SubscriptionUsageDto>, IAuditableRequest
{
    public string Action => "Subscription.Reactivated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => null;
}
