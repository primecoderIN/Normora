using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

public class CreateCheckoutSessionCommandHandler(
    TenantsDbContext context,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IPaymentService paymentService) : IRequestHandler<CreateCheckoutSessionCommand, CheckoutSessionResultDto>
{
    public async Task<CheckoutSessionResultDto> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve current active tenant from scoped ambient context
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        // 2. Fetch workspace details (name and slug) for return URLs and customer details
        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException(ApiMessages.NotFound);

        var userEmail = currentUser.Email ?? string.Empty;

        // 3. Request payment service to generate hosted checkout parameters with signature
        var session = await paymentService.CreatePaymentSessionAsync(
            tenantId,
            tenant.Name,
            tenant.Slug,
            userEmail,
            request.Tier,
            request.Currency,
            cancellationToken);

        return new CheckoutSessionResultDto(session.ActionUrl, session.Params);
    }
}

