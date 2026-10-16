namespace Normora.Shared.Options;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    public string ProPriceId { get; set; } = string.Empty;
    public string EnterprisePriceId { get; set; } = string.Empty;

    public string SuccessUrl { get; set; } = "http://localhost:4200/settings/subscription?status=success&session_id={CHECKOUT_SESSION_ID}";
    public string CancelUrl { get; set; } = "http://localhost:4200/settings/subscription?status=cancelled";

    /// <summary>
    /// Indicates whether valid live/test Stripe keys have been configured.
    /// When false, the application falls back to a sandbox simulated checkout flow for local development.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey) &&
                                !SecretKey.StartsWith("placeholder", StringComparison.OrdinalIgnoreCase) &&
                                !SecretKey.StartsWith("mock", StringComparison.OrdinalIgnoreCase);
}
