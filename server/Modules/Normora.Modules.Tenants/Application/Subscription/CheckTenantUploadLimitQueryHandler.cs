using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Contracts.Tenants;
using Normora.Shared.Exceptions;

namespace Normora.Modules.Tenants.Application.Subscription;

/// <summary>
/// Verifies tenant subscription boundaries before an upload proceeds.
/// </summary>
public class CheckTenantUploadLimitQueryHandler(
    TenantsDbContext context,
    IMediator mediator) : IRequestHandler<CheckTenantUploadLimitQuery, bool>
{
    public async Task<bool> Handle(CheckTenantUploadLimitQuery request, CancellationToken cancellationToken)
    {
        var plan = await context.TenantPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == request.TenantId, cancellationToken)
            ?? TenantPlan.CreateDefault(request.TenantId);

        var usage = await mediator.Send(new GetTenantDocumentUsageQuery(request.TenantId), cancellationToken);

        if (request.IsNewDocument && usage.DocumentCount >= plan.MaxDocuments)
        {
            throw new PlanLimitExceededException(ApiMessages.PlanDocumentLimitExceeded);
        }

        if (usage.TotalStorageBytes + request.IncomingFileSizeBytes > plan.MaxStorageBytes)
        {
            throw new PlanLimitExceededException(ApiMessages.PlanStorageLimitExceeded);
        }

        return true;
    }
}
