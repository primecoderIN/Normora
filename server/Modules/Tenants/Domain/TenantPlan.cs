namespace Normora.Modules.Tenants.Domain;

public enum PlanTier
{
    Free = 0,
    Pro = 1,
    Enterprise = 2
}

/// <summary>
/// Defines the subscription tier and resource limits for a tenant.
/// </summary>
public class TenantPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public PlanTier PlanTier { get; set; } = PlanTier.Free;
    public int MaxDocuments { get; set; } = 10;
    public int MaxEmployees { get; set; } = 5;
    public long MaxStorageBytes { get; set; } = 52_428_800; // 50 MB
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Billing currency (e.g. "inr", "usd", "eur") - defaults to Indian Rupees
    public string Currency { get; set; } = "inr";

    // Payment Gateway integration
    public string? PaymentTransactionId { get; set; }
    public string? PaymentReferenceId { get; set; }

    public string? SubscriptionStatus { get; set; } // "active", "trialing", "past_due", "canceled"
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }

    public Tenant? Tenant { get; set; }

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
