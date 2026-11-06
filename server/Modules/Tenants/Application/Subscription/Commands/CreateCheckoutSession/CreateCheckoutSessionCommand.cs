using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

public record CreateCheckoutSessionCommand(PlanTier Tier, string? Currency = "inr") : IRequest<CheckoutSessionResultDto>, IAuditableRequest
{
    public string Action => "Subscription.CheckoutInitiated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { tier = Tier.ToString(), currency = Currency });
}

