namespace Normora.Shared.Options;

/// <summary>
/// Configuration options for the transactional email subsystem.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Delivery provider: "Resend" (default), or "Null" (for testing/disabled email).
    /// </summary>
    public string Provider { get; set; } = "Resend";

    /// <summary>
    /// API key for the transactional email provider (e.g. Resend, SendGrid, etc.).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Alias for backwards compatibility with configuration keys named ResendApiKey or RESEND_API_KEY.
    /// </summary>
    public string? ResendApiKey
    {
        get => ApiKey;
        set => ApiKey = value;
    }

    /// <summary>
    /// Default sender email address (e.g. "onboarding@resend.dev" or your verified domain address).
    /// </summary>
    public string FromEmail { get; set; } = "onboarding@resend.dev";

    /// <summary>
    /// Friendly sender display name (e.g., "Normora").
    /// </summary>
    public string FromName { get; set; } = "Normora";

    /// <summary>
    /// Optional developer email address to redirect all outbound emails to during development/testing.
    /// Particularly useful with sandbox providers like Resend free tier that only allow delivery to the account owner.
    /// </summary>
    public string? DevOverrideRecipient { get; set; }
}
