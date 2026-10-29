namespace Normora.Modules.Tenants.Options;

/// <summary>Holds merchant credentials and endpoint selection for PayU hosted payments.</summary>
public class PayUOptions
{
    public const string SectionName = "PayU";

    /// <summary>
    /// Merchant Key provided by PayU (e.g. gtKFFx for PayU test environment).
    /// </summary>
    public string MerchantKey { get; set; } = "gtKFFx";

    /// <summary>
    /// Merchant Salt provided by PayU for SHA-512 cryptographic hash generation.
    /// </summary>
    public string MerchantSalt { get; set; } = "eCwWELxi";

    /// <summary>
    /// True for live PayU production environment, false for PayU test sandbox.
    /// </summary>
    public bool IsProduction { get; set; } = false;

    /// <summary>
    /// PayU hosted payment gateway endpoint.
    /// </summary>
    public string PaymentUrl => IsProduction
        ? "https://secure.payu.in/_payment"
        : "https://test.payu.in/_payment";

    /// <summary>
    /// Indicates whether merchant key and salt have been configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(MerchantKey) &&
                                !string.IsNullOrWhiteSpace(MerchantSalt) &&
                                !MerchantKey.StartsWith("placeholder", StringComparison.OrdinalIgnoreCase);
}
