using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

public class CreateCustomerPortalSessionCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    IOptions<AppOptions> appOptions) : IRequestHandler<CreateCustomerPortalSessionCommand, CustomerPortalResultDto>
{
    public async Task<CustomerPortalResultDto> Handle(CreateCustomerPortalSessionCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve current active tenant from scoped ambient context
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        // 2. Fetch workspace slug to construct workspace-specific return URL
        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        // 3. Direct to subscription settings view for invoice and billing overview
        var returnUrl = !string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? request.ReturnUrl
            : (tenant != null
                ? $"{appOptions.Value.BaseUrl}/app/workspaces/{tenant.Slug}/employer/settings/subscription"
                : $"{appOptions.Value.BaseUrl}/settings/subscription");

        return new CustomerPortalResultDto(returnUrl);
    }
}

