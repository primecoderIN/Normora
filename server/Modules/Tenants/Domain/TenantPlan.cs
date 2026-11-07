namespace Normora.Modules.Tenants.Domain;

/// <summary>Identifies the subscription level that controls tenant resource limits.</summary>
public enum PlanTier
{
    Free = 0,
    Pro = 1,
    Enterprise = 2
}

/// <summary>
/// Domain Entity encapsulating tenant subscription tier quotas and payment metadata.
/// Governs system boundaries checked prior to document ingestion, employee invitations,
/// and file uploads.
/// </summary>
public class TenantPlan : Normora.Shared.Interfaces.ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>Subscription tier level: Free (0), Pro (1), Enterprise (2).</summary>
    public PlanTier PlanTier { get; set; } = PlanTier.Free;

    /// <summary>Maximum distinct root documents permitted in the knowledge base.</summary>
    public int MaxDocuments { get; set; } = 10;

    /// <summary>Maximum team members permitted in the workspace (active members + pending invites).</summary>
    public int MaxEmployees { get; set; } = 5;

    /// <summary>Maximum cumulative binary storage bytes across all uploaded document versions.</summary>
    public long MaxStorageBytes { get; set; } = 52_428_800; // 50 MB

    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Billing currency (e.g. "inr", "usd", "eur") - defaults to Indian Rupees
    public string Currency { get; set; } = "inr";

    // Payment Gateway integration (PayU India)
    public string? PaymentTransactionId { get; set; }
    public string? PaymentReferenceId { get; set; }

    public string? SubscriptionStatus { get; set; } // "active", "trialing", "past_due", "canceled"
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }

    public Tenant? Tenant { get; set; }

    /// <summary>
    /// Factory template method generating default quota thresholds for the specified plan tier.
    /// - Free: 10 docs, 5 employees, 50MB storage.
    /// - Pro: 100 docs, 50 employees, 5GB storage.
    /// - Enterprise: 10,000 docs, 5,000 employees, 500GB storage.
    /// </summary>
    public static TenantPlan CreateDefault(Guid tenantId, PlanTier tier = PlanTier.Free, string currency = "inr") => tier switch
    {
        PlanTier.Pro => new TenantPlan
        {
            TenantId = tenantId,
            PlanTier = PlanTier.Pro,
            MaxDocuments = 100,
            MaxEmployees = 50,
            MaxStorageBytes = 5_368_709_120, // 5 GB
            Currency = string.IsNullOrWhiteSpace(currency) ? "inr" : currency.ToLowerInvariant()
        },
        PlanTier.Enterprise => new TenantPlan
        {
            TenantId = tenantId,
            PlanTier = PlanTier.Enterprise,
            MaxDocuments = 10_000,
            MaxEmployees = 5_000,
            MaxStorageBytes = 536_870_912_000, // 500 GB
            Currency = string.IsNullOrWhiteSpace(currency) ? "inr" : currency.ToLowerInvariant()
        },
        _ => new TenantPlan
        {
            TenantId = tenantId,
            PlanTier = PlanTier.Free,
            MaxDocuments = 10,
            MaxEmployees = 5,
            MaxStorageBytes = 52_428_800, // 50 MB
            Currency = string.IsNullOrWhiteSpace(currency) ? "inr" : currency.ToLowerInvariant()
        }
    };
}
