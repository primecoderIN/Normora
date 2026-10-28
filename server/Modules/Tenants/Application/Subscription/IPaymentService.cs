using Microsoft.AspNetCore.Http;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.Subscription;

public record PaymentCheckoutDto(
    string ActionUrl,
    Dictionary<string, string> Params
);

public record PaymentCallbackResultDto(
    bool Success,
    Guid TenantId,
    string TenantSlug,
    PlanTier Tier,
    string TransactionId,
    string? ErrorMessage
);

public interface IPaymentService
{
    /// <summary>
    /// Generates hosted checkout request parameters and signature for upgrading workspace tier.
    /// </summary>
    Task<PaymentCheckoutDto> CreatePaymentSessionAsync(
        Guid tenantId,
        string tenantName,
        string tenantSlug,
        string userEmail,
        PlanTier tier,
        string? currency = "inr",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cryptographically validates the callback from the payment provider and fulfills the subscription.
    /// </summary>
    Task<PaymentCallbackResultDto> ProcessCallbackAsync(
        IFormCollection form,
        CancellationToken cancellationToken = default);
}
