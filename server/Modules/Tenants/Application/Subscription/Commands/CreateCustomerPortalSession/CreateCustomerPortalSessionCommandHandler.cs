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
        // Validates return URL to prevent open redirect vulnerabilities.
        // Accepts only safe relative URLs or absolute URLs matching AppOptions.BaseUrl.
        string returnUrl;
        var defaultUrl = tenant != null
            ? $"{appOptions.Value.BaseUrl}/app/workspaces/{tenant.Slug}/employer/settings/subscription"
            : $"{appOptions.Value.BaseUrl}/settings/subscription";

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl))
        {
            var isSafeRelative = request.ReturnUrl.StartsWith('/') && !request.ReturnUrl.StartsWith("//") && !request.ReturnUrl.StartsWith("/\\");
            var isSafeAbsolute = Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out var parsedUri)
                && Uri.TryCreate(appOptions.Value.BaseUrl, UriKind.Absolute, out var baseUri)
                && string.Equals(parsedUri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase);

            returnUrl = (isSafeRelative || isSafeAbsolute) ? request.ReturnUrl : defaultUrl;
        }
        else
        {
            returnUrl = defaultUrl;
        }

        return new CustomerPortalResultDto(returnUrl);
    }
}

