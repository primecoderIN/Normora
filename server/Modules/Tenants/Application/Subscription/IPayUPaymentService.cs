using Microsoft.AspNetCore.Http;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.Subscription;

public record PayUCheckoutDto(
    string ActionUrl,
    Dictionary<string, string> Params
);

public record PayUCallbackResultDto(
    bool Success,
    Guid TenantId,
    string TenantSlug,
    PlanTier Tier,
    string TransactionId,
    string? ErrorMessage
);

public interface IPayUPaymentService
{
    /// <summary>
    /// Generates PayU hosted checkout request parameters and SHA-512 hash for upgrading workspace tier.
    /// Supports UPI for domestic Indian payments and Credit/Debit Cards for international and domestic payments.
    /// </summary>
    Task<PayUCheckoutDto> CreatePaymentSessionAsync(
        Guid tenantId,
        string tenantName,
        string tenantSlug,
        string userEmail,
        PlanTier tier,
        string? currency = "inr",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cryptographically validates the reverse SHA-512 signature of PayU callback and fulfills the subscription.
    /// </summary>
    Task<PayUCallbackResultDto> ProcessCallbackAsync(
        IFormCollection form,
        CancellationToken cancellationToken = default);
}
