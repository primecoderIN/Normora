using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.Subscription;
using Normora.Modules.Tenants.Domain;
using Normora.Shared;
using Normora.Shared.Constants;

namespace Normora.Api.Controllers;

public record UpdatePlanTierRequest(PlanTier Tier);
public record CreateCheckoutRequest(PlanTier Tier);
public record CreatePortalRequest(string? ReturnUrl = null);

/// <summary>
/// Handles subscription plan limits, quota metrics, and tier management.
/// </summary>
[ApiController]
[Route("api/subscription")]
[Produces("application/json")]
public class SubscriptionController(
    IMediator mediator,
    IStripePaymentService stripePaymentService) : ControllerBase
{
    /// <summary>
    /// Retrieves current resource usage and subscription boundaries for the active tenant.
    /// Dual-fulfillment: if sessionId is passed (from Checkout return redirect), it synchronously
    /// verifies payment status with Stripe and updates quotas immediately before returning usage metrics.
    /// </summary>
    [HttpGet("usage")]
    [Authorize]
    [RequireTenant]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<SubscriptionUsageDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUsage([FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        // Dispatches query to Tenant module; handles both standard usage queries and synchronous post-checkout fulfillment
        var result = await mediator.Send(new GetTenantSubscriptionUsageQuery(sessionId), cancellationToken);
        return Ok(ApiResponse<SubscriptionUsageDto>.Ok(result));
    }

    /// <summary>
    /// Creates a Stripe Hosted Checkout Session for upgrading to a paid tier (Pro / Enterprise).
    /// BFLA: restricted strictly to tenant administrators.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<CheckoutSessionResultDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        // Builds Stripe checkout session embedding tenant_id metadata and returns hosted redirect URL
        var result = await mediator.Send(new CreateCheckoutSessionCommand(request.Tier), cancellationToken);
        return Ok(ApiResponse<CheckoutSessionResultDto>.Ok(result, ApiMessages.CheckoutSessionCreated));
    }

    /// <summary>
    /// Creates a Stripe Billing Customer Portal session for managing invoices, payment methods, and subscriptions.
    /// BFLA: restricted strictly to tenant administrators.
    /// </summary>
    [HttpPost("portal")]
    [Authorize]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<CustomerPortalResultDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> CreatePortal([FromBody] CreatePortalRequest? request, CancellationToken cancellationToken)
    {
        // Generates self-service billing portal session URL with workspace return URL
        var result = await mediator.Send(new CreateCustomerPortalSessionCommand(request?.ReturnUrl), cancellationToken);
        return Ok(ApiResponse<CustomerPortalResultDto>.Ok(result, ApiMessages.CustomerPortalCreated));
    }

    /// <summary>
    /// Receives asynchronous Stripe Webhook notifications with HMAC signature validation.
    /// Public endpoint (AllowAnonymous + IgnoreAntiforgeryToken) secured solely via cryptographic HMAC Stripe-Signature.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        // Extract Stripe signature header for cryptographic HMAC-SHA256 validation
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);

        // Verify payload integrity and dispatch lifecycle events (checkout completed, subscription updated, etc.)
        var success = await stripePaymentService.HandleWebhookAsync(json, signature, cancellationToken);
        if (!success)
        {
            return BadRequest();
        }

        return Ok();
    }

    /// <summary>
    /// Updates or downgrades the subscription plan tier directly (used for manual or free tier management).
    /// BFLA: restricted strictly to tenant administrators.
    /// </summary>
    [HttpPost("tier")]
    [Authorize]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<SubscriptionUsageDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateTier([FromBody] UpdatePlanTierRequest request, CancellationToken cancellationToken)
    {
        // Directly updates tenant plan limits and returns freshly computed usage metrics
        var result = await mediator.Send(new UpdateTenantPlanTierCommand(request.Tier), cancellationToken);
        return Ok(ApiResponse<SubscriptionUsageDto>.Ok(result, ApiMessages.SubscriptionUpdated));
    }
}
