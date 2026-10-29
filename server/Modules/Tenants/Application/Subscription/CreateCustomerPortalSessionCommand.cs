using MediatR;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;
using Microsoft.Extensions.Options;

using Microsoft.EntityFrameworkCore;

namespace Normora.Modules.Tenants.Application.Subscription;

/// <summary>Creates a return URL for the active tenant's payment-provider billing portal.</summary>
public record CustomerPortalResultDto(string PortalUrl);

public record CreateCustomerPortalSessionCommand(string? ReturnUrl = null) : IRequest<CustomerPortalResultDto>, IAuditableRequest
{
    public string Action => "Subscription.PortalInitiated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => null;
}

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
