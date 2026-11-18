using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.UpdateTenantCurrency;

/// <summary>
/// Command to update the default billing currency preference for an active tenant workspace.
/// Handled by <see cref="UpdateTenantCurrencyCommandHandler"/>.
/// </summary>
public record UpdateTenantCurrencyCommand(string Currency) : IRequest<SubscriptionUsageDto>, IAuditableRequest
{
    public string Action => "Subscription.CurrencyUpdated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { currency = Currency });
}
