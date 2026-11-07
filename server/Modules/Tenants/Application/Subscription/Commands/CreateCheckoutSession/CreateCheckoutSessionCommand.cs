using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

/// <summary>
/// Command to initialize a hosted payment checkout session (PayU) for upgrading or changing the tenant's tier.
/// Handled by <see cref="CreateCheckoutSessionCommandHandler"/>.
/// </summary>
/// <remarks>
/// Billing Architecture:
/// - Verifies that the initiating user is an Owner or Admin of the ambient tenant (BFLA protection).
/// - Assembles required PayU gateway parameters including cryptographic SHA-512 payment hash signatures.
/// - Supplies callback endpoints (/api/subscription/payu-callback) to handle asynchronous payment success/failure.
/// - Implements <see cref="IAuditableRequest"/> to record checkout initiation in the audit trail.
/// </remarks>
/// <param name="Tier">The requested target subscription plan tier (Starter, Professional, Enterprise).</param>
/// <param name="Currency">Target billing currency code, defaulting to "inr" (Indian Rupee) for PayU.</param>
public record CreateCheckoutSessionCommand(PlanTier Tier, string? Currency = "inr") : IRequest<CheckoutSessionResultDto>, IAuditableRequest
{
    public string Action => "Subscription.CheckoutInitiated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { tier = Tier.ToString(), currency = Currency });
}

