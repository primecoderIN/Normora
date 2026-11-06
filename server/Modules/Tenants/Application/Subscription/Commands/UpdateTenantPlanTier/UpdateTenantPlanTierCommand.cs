using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

/// <summary>Changes the active tenant's plan tier and returns its updated quota usage.</summary>
public record UpdateTenantPlanTierCommand(PlanTier Tier) : IRequest<SubscriptionUsageDto>, IAuditableRequest
{
    public string Action => "Subscription.TierUpdated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { tier = Tier.ToString() });
}

