using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Contracts.Tenants;
using Normora.Shared.Exceptions;

namespace Normora.Modules.Tenants.Application.Subscription.Queries;

/// <summary>
/// Verifies tenant subscription boundaries before an upload proceeds.
/// </summary>
public class CheckTenantUploadLimitQueryHandler(
    TenantsDbContext context,
    IMediator mediator) : IRequestHandler<CheckTenantUploadLimitQuery, bool>
{
    public async Task<bool> Handle(CheckTenantUploadLimitQuery request, CancellationToken cancellationToken)
    {
        // Tenant Subscription Plan Boundary Verification:
        // Before any document upload or version creation is processed in the Documents module,
        // this cross-module handler checks whether the tenant has exceeded its document count or storage limits.
        var plan = await context.TenantPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == request.TenantId, cancellationToken)
            ?? TenantPlan.CreateDefault(request.TenantId);

        // Cross-Module Query:
        // Ask the Documents module for the tenant's current total document count and disk storage bytes.
        var usage = await mediator.Send(new GetTenantDocumentUsageQuery(request.TenantId), cancellationToken);

        // Invariant: Max Document Count Check
        // If this is a brand-new document (not a new version of an existing document) and document quota is maxed,
        // throw PlanLimitExceededException (which maps to HTTP 402 Payment Required).
        if (request.IsNewDocument && usage.DocumentCount >= plan.MaxDocuments)
        {
            throw new PlanLimitExceededException(ApiMessages.PlanDocumentLimitExceeded);
        }

        // Invariant: Max Storage Bytes Check
        // Enforce the storage byte limit across all documents and versions including the incoming payload size.
        if (usage.TotalStorageBytes + request.IncomingFileSizeBytes > plan.MaxStorageBytes)
        {
            throw new PlanLimitExceededException(ApiMessages.PlanStorageLimitExceeded);
        }

        return true;
    }
}
