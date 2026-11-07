namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

/// <summary>
/// Combines the active tenant's plan tier entitlements with real-time resource consumption metrics.
/// Powers the subscription usage progress bars, upgrade banners, and limit warnings on the UI.
/// </summary>
public record SubscriptionUsageDto(
    // Current plan tier name ("Free", "Starter", "Professional", "Enterprise").
    string PlanTier,

    // Maximum document uploads permitted by the active tier.
    int MaxDocuments,

    // Current non-deleted document count owned by the tenant.
    int CurrentDocuments,

    // Maximum registered employee seats permitted.
    int MaxEmployees,

    // Current active employee membership count.
    int CurrentEmployees,

    // Storage capacity ceiling in bytes.
    long MaxStorageBytes,

    // Current consumed storage volume in bytes across all uploaded versions.
    long CurrentStorageBytes,

    // Expiration date of the current subscription period (null for free/lifetime).
    DateTime? ValidUntil,

    // Flag indicating whether document count has met or exceeded the tier ceiling.
    bool IsDocumentLimitReached,

    // Flag indicating whether employee seat limit has met or exceeded the tier ceiling.
    bool IsEmployeeLimitReached,

    // Flag indicating whether storage bytes have met or exceeded the tier ceiling.
    bool IsStorageLimitReached,

    // Whether the tenant possesses a paid active subscription status.
    bool HasActiveSubscription,

    // Raw status string from the billing gateway (e.g. "active", "past_due", "canceled").
    string? SubscriptionStatus,

    // UTC end timestamp for the current recurring billing cycle.
    DateTime? CurrentPeriodEndUtc,

    // Whether the subscription is scheduled for cancellation at the end of the period.
    bool CancelAtPeriodEnd,

    // Billing currency code (e.g. "inr").
    string Currency
);

