using Microsoft.AspNetCore.Http;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.Subscription;

/// <summary>
/// Encapsulates PayU hosted checkout form parameters and target gateway endpoint.
/// </summary>
public record PaymentCheckoutDto(
    // The target POST URL for the PayU hosted payment page.
    string ActionUrl,

    // Key-value pairs containing transaction details, return URLs, and calculated SHA-512 hashes.
    Dictionary<string, string> Params
);

/// <summary>
/// Encapsulates the verified outcome of an asynchronous payment gateway callback.
/// </summary>
public record PaymentCallbackResultDto(
    // Whether the transaction was successfully authorized and signature verified.
    bool Success,

    // The unique identifier of the tenant associated with this transaction.
    Guid TenantId,

    // The tenant's vanity slug for redirection back into their workspace.
    string TenantSlug,

    // The subscription tier that was purchased.
    PlanTier Tier,

    // Gateway transaction identifier issued by PayU.
    string TransactionId,

    // Error description if the payment failed or hash verification failed.
    string? ErrorMessage
);

/// <summary>
/// Domain service interface abstracting payment gateway integrations (PayU).
/// Decouples CQRS command handlers from third-party HTTP and cryptographic hashing mechanics.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Generates hosted checkout request parameters and signature for upgrading workspace tier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the upgrading tenant.</param>
    /// <param name="tenantName">Display name of the organization.</param>
    /// <param name="tenantSlug">Unique vanity slug for return routing.</param>
    /// <param name="userEmail">Billing contact email address.</param>
    /// <param name="tier">The target subscription tier.</param>
    /// <param name="currency">Currency code (defaults to "inr").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
    /// Validates reverse SHA-512 hash signatures to protect against payment spoofing attacks.
    /// </summary>
    /// <param name="form">Form collection submitted by the PayU browser redirect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PaymentCallbackResultDto> ProcessCallbackAsync(
        IFormCollection form,
        CancellationToken cancellationToken = default);
}
