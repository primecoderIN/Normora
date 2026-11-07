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
        // Ambient Tenant Verification:
        // Billing operations must always execute within the context of an identified tenant workspace.
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        // Workspace Metadata Retrieval:
        // We retrieve tenant details (display name and URL slug) without EF tracking to populate
        // transaction metadata, billing descriptors, and return/cancel callback URLs.
        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException(ApiMessages.NotFound);

        var userEmail = currentUser.Email ?? string.Empty;

        // Payment Gateway Integration (PayU India):
        // Calls IPaymentService to generate the hosted checkout form parameters and HMAC-SHA512 checksum.
        // The resulting payload allows the frontend client to submit a secure form post directly to the gateway.
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

