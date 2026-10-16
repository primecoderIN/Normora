using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.Subscription;

public interface IStripePaymentService
{
    /// <summary>
    /// Creates a Stripe Checkout Session for upgrading or subscribing to a paid tier.
    /// Returns the hosted checkout URL to which the client should be redirected.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        Guid tenantId,
        string tenantName,
        string userEmail,
        PlanTier tier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Stripe Billing Customer Portal session for managing invoices, payment methods, or cancelling.
    /// Returns the portal session URL.
    /// </summary>
    Task<string> CreateCustomerPortalSessionAsync(
        Guid tenantId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Securely validates HMAC signature and processes Stripe webhook events (checkout.session.completed,
    /// customer.subscription.updated/deleted, invoice.payment_failed).
    /// </summary>
    Task<bool> HandleWebhookAsync(
        string jsonPayload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default);
}
