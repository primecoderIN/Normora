using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Queries.GetTenantBillingDetails;

public class GetTenantBillingDetailsQueryHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : IRequestHandler<GetTenantBillingDetailsQuery, TenantBillingDetailsDto>
{
    public async Task<TenantBillingDetailsDto> Handle(GetTenantBillingDetailsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var billing = await context.TenantBillingDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

        if (billing != null)
        {
            return new TenantBillingDetailsDto(
                billing.BillingEmail,
                billing.CompanyLegalName,
                billing.TaxId,
                billing.BillingAddress
            );
        }

        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        return new TenantBillingDetailsDto(
            currentUser.Email ?? "billing@normora.internal",
            tenant?.Name ?? "Workspace Organization",
            "GSTIN29ABCDE1234F1Z5",
            "Level 4, Prestige Tech Park, Bangalore 560103, India"
        );
    }
}

