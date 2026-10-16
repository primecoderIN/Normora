using MediatR;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;
using Microsoft.Extensions.Options;

namespace Normora.Modules.Tenants.Application.Subscription;

public record CustomerPortalResultDto(string PortalUrl);

public record CreateCustomerPortalSessionCommand(string? ReturnUrl = null) : IRequest<CustomerPortalResultDto>, IAuditableRequest
{
    public string Action => "Subscription.PortalInitiated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => null;
}

public class CreateCustomerPortalSessionCommandHandler(
    ITenantContext tenantContext,
    IStripePaymentService stripeService,
    IOptions<AppOptions> appOptions) : IRequestHandler<CreateCustomerPortalSessionCommand, CustomerPortalResultDto>
{
    public async Task<CustomerPortalResultDto> Handle(CreateCustomerPortalSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var returnUrl = !string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? request.ReturnUrl
            : $"{appOptions.Value.BaseUrl}/settings/subscription";

        var portalUrl = await stripeService.CreateCustomerPortalSessionAsync(
            tenantId,
            returnUrl,
            cancellationToken);

        return new CustomerPortalResultDto(portalUrl);
    }
}
