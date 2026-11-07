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
        // Ambient Tenant Verification:
        // Validates that billing management executes within an active tenant workspace.
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        // Workspace Metadata Lookup:
        // Fetches the tenant slug to route the user back to their workspace billing settings tab.
        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        // Customer Portal Navigation:
        // Constructs the canonical destination URL pointing to the workspace's subscription settings view,
        // allowing administrators to view current usage quotas, upgrade plans, or inspect transaction history.
        var returnUrl = !string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? request.ReturnUrl
            : (tenant != null
                ? $"{appOptions.Value.BaseUrl}/app/workspaces/{tenant.Slug}/employer/settings/subscription"
                : $"{appOptions.Value.BaseUrl}/settings/subscription");

        return new CustomerPortalResultDto(returnUrl);
    }
}

