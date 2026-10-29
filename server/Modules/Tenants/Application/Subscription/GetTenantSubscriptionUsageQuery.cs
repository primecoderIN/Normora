using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription;

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

public record GetTenantSubscriptionUsageQuery() : IRequest<SubscriptionUsageDto>;

public class GetTenantSubscriptionUsageQueryHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<GetTenantSubscriptionUsageQuery, SubscriptionUsageDto>
{
    public async Task<SubscriptionUsageDto> Handle(GetTenantSubscriptionUsageQuery request, CancellationToken cancellationToken)
    {
        // 1. Resolve active tenant from ambient context
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        // 2. Retrieve tenant's active plan limits or fallback to default Starter/Free plan
        var plan = await context.TenantPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (plan == null)
        {
            plan = TenantPlan.CreateDefault(tenantId);
        }

        // 3. Calculate active team members count (ignoring soft-deleted memberships)
        var currentEmployees = await context.TenantMemberships
            .CountAsync(m => m.TenantId == tenantId && m.RemovedAt == null, cancellationToken);

        // 4. Query Documents module via cross-module MediatR contract for current document count and storage bytes
        var docUsage = await mediator.Send(new GetTenantDocumentUsageQuery(tenantId), cancellationToken);

        // 5. Evaluate boundary thresholds for UI quota progress bars and warnings
        var isDocLimitReached = docUsage.DocumentCount >= plan.MaxDocuments;
        var isEmpLimitReached = currentEmployees >= plan.MaxEmployees;
        var isStorageLimitReached = docUsage.TotalStorageBytes >= plan.MaxStorageBytes;
        var hasActiveSubscription = !string.IsNullOrWhiteSpace(plan.PaymentTransactionId) && plan.PlanTier != PlanTier.Free;

        return new SubscriptionUsageDto(
            plan.PlanTier.ToString(),
            plan.MaxDocuments,
            docUsage.DocumentCount,
            plan.MaxEmployees,
            currentEmployees,
            plan.MaxStorageBytes,
            docUsage.TotalStorageBytes,
            plan.ValidUntil,
            isDocLimitReached,
            isEmpLimitReached,
            isStorageLimitReached,
            hasActiveSubscription,
            plan.SubscriptionStatus,
            plan.CurrentPeriodEndUtc,
            plan.CancelAtPeriodEnd,
            string.IsNullOrWhiteSpace(plan.Currency) ? "inr" : plan.Currency.ToLowerInvariant()
        );
    }
}
