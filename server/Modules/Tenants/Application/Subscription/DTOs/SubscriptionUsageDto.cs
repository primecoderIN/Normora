namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

/// <summary>Combines the tenant's plan limits with current member, document, and storage usage.</summary>
public record SubscriptionUsageDto(
    string PlanTier,
    int MaxDocuments,
    int CurrentDocuments,
    int MaxEmployees,
    int CurrentEmployees,
    long MaxStorageBytes,
    long CurrentStorageBytes,
    DateTime? ValidUntil,
    bool IsDocumentLimitReached,
    bool IsEmployeeLimitReached,
    bool IsStorageLimitReached,
    bool HasActiveSubscription,
    string? SubscriptionStatus,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    string Currency
);

