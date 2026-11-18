using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.ToggleAutoRenew;

/// <summary>
/// Command to enable or disable recurring auto-debit renewals for the workspace subscription.
/// </summary>
/// <param name="Enabled">Whether auto-renewal should be enabled or disabled.</param>
public record ToggleAutoRenewCommand(bool Enabled) : IRequest<SubscriptionUsageDto>, IAuditableRequest
{
    public string Action => "Subscription.AutoRenewToggled";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { enabled = Enabled });
}
