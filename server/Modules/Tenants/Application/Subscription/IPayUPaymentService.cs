using Microsoft.AspNetCore.Http;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.Subscription;

/// <summary>Defines PayU-specific payloads and operations while extending the provider-neutral contract.</summary>
public record PayUCheckoutDto(string ActionUrl, Dictionary<string, string> Params) : PaymentCheckoutDto(ActionUrl, Params);

public record PayUCallbackResultDto(
    bool Success,
    Guid TenantId,
    string TenantSlug,
    PlanTier Tier,
    string TransactionId,
    string? ErrorMessage
) : PaymentCallbackResultDto(Success, TenantId, TenantSlug, Tier, TransactionId, ErrorMessage);

public interface IPayUPaymentService : IPaymentService
{
}
