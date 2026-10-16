using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription;

public record CheckoutSessionResultDto(string CheckoutUrl);

public record CreateCheckoutSessionCommand(PlanTier Tier) : IRequest<CheckoutSessionResultDto>, IAuditableRequest
{
    public string Action => "Subscription.CheckoutInitiated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { tier = Tier.ToString() });
}

public class CreateCheckoutSessionCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IStripePaymentService stripeService) : IRequestHandler<CreateCheckoutSessionCommand, CheckoutSessionResultDto>
{
    public async Task<CheckoutSessionResultDto> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException(ApiMessages.NotFound);

        var userEmail = currentUser.Email ?? string.Empty;

        var checkoutUrl = await stripeService.CreateCheckoutSessionAsync(
            tenantId,
            tenant.Name,
            tenant.Slug,
            userEmail,
            request.Tier,
            cancellationToken);

        return new CheckoutSessionResultDto(checkoutUrl);
    }
}
