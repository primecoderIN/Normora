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
    /// </summary>
    [HttpGet("usage")]
    [Authorize]
    [RequireTenant]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<SubscriptionUsageDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUsage([FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTenantSubscriptionUsageQuery(sessionId), cancellationToken);
        return Ok(ApiResponse<SubscriptionUsageDto>.Ok(result));
    }

    /// <summary>
    /// Creates a Stripe Hosted Checkout Session for upgrading to a paid tier (Pro / Enterprise).
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
        var result = await mediator.Send(new CreateCheckoutSessionCommand(request.Tier), cancellationToken);
        return Ok(ApiResponse<CheckoutSessionResultDto>.Ok(result, ApiMessages.CheckoutSessionCreated));
    }

    /// <summary>
    /// Creates a Stripe Billing Customer Portal session for managing invoices, payment methods, and subscriptions.
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
        var result = await mediator.Send(new CreateCustomerPortalSessionCommand(request?.ReturnUrl), cancellationToken);
        return Ok(ApiResponse<CustomerPortalResultDto>.Ok(result, ApiMessages.CustomerPortalCreated));
    }

    /// <summary>
    /// Receives asynchronous Stripe Webhook notifications with HMAC signature validation.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);

        var success = await stripePaymentService.HandleWebhookAsync(json, signature, cancellationToken);
        if (!success)
        {
            return BadRequest();
        }

        return Ok();
    }

    /// <summary>
    /// Updates or downgrades the subscription plan tier directly (used for manual or free tier management).
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
        var result = await mediator.Send(new UpdateTenantPlanTierCommand(request.Tier), cancellationToken);
        return Ok(ApiResponse<SubscriptionUsageDto>.Ok(result, ApiMessages.SubscriptionUpdated));
    }
}
