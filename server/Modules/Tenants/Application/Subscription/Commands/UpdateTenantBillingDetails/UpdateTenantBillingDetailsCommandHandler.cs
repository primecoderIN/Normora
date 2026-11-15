using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.UpdateTenantBillingDetails;

public class UpdateTenantBillingDetailsCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext) : IRequestHandler<UpdateTenantBillingDetailsCommand, TenantBillingDetailsDto>
{
    public async Task<TenantBillingDetailsDto> Handle(UpdateTenantBillingDetailsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var billing = await context.TenantBillingDetails
            .FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);

        if (billing == null)
        {
            billing = new TenantBillingDetail
            {
                TenantId = tenantId,
                BillingEmail = request.BillingEmail.Trim(),
                CompanyLegalName = request.CompanyLegalName.Trim(),
                TaxId = request.TaxId.Trim(),
                BillingAddress = request.BillingAddress.Trim(),
                UpdatedAt = DateTime.UtcNow
            };
            context.TenantBillingDetails.Add(billing);
        }
        else
        {
            billing.BillingEmail = request.BillingEmail.Trim();
            billing.CompanyLegalName = request.CompanyLegalName.Trim();
            billing.TaxId = request.TaxId.Trim();
            billing.BillingAddress = request.BillingAddress.Trim();
            billing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new TenantBillingDetailsDto(
            billing.BillingEmail,
            billing.CompanyLegalName,
            billing.TaxId,
            billing.BillingAddress
        );
    }
}

