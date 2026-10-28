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
public record CreateCheckoutRequest(PlanTier Tier, string? Currency = null);
public record CreatePortalRequest(string? ReturnUrl = null);

/// <summary>
/// Handles subscription plan limits, quota metrics, and tier management.
/// </summary>
[ApiController]
[Route("api/subscription")]
[Produces("application/json")]
public class SubscriptionController(
    IMediator mediator,
    IPaymentService paymentService,
    Microsoft.Extensions.Options.IOptions<Normora.Shared.Options.AppOptions> appOptions) : ControllerBase
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
    public async Task<IActionResult> GetUsage(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);
        return Ok(ApiResponse<SubscriptionUsageDto>.Ok(result));
    }

    /// <summary>
    /// Generates PayU checkout parameters and cryptographic SHA-512 signature for upgrading to a paid tier.
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
        var result = await mediator.Send(new CreateCheckoutSessionCommand(request.Tier, request.Currency), cancellationToken);
        return Ok(ApiResponse<CheckoutSessionResultDto>.Ok(result, ApiMessages.CheckoutSessionCreated));
    }

    /// <summary>
    /// Directs user to workspace subscription and billing management overview.
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
        var result = await mediator.Send(new CreateCustomerPortalSessionCommand(request?.ReturnUrl), cancellationToken);
        return Ok(ApiResponse<CustomerPortalResultDto>.Ok(result, ApiMessages.CustomerPortalCreated));
    }

    /// <summary>
    /// Handles payment provider callback response (e.g. PayU surl/furl) with cryptographic verification.
    /// Public return endpoint called by payment gateway upon completion of checkout.
    /// </summary>
    [HttpPost("callback")]
    [HttpPost("payu-callback")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandlePaymentCallback(CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        var result = await paymentService.ProcessCallbackAsync(form, cancellationToken);

        var baseUri = new Uri(appOptions.Value.BaseUrl, UriKind.Absolute);
        var destinationPath = !string.IsNullOrWhiteSpace(result.TenantSlug)
            ? $"app/workspaces/{Uri.EscapeDataString(result.TenantSlug)}/employer/settings/subscription"
            : "settings/subscription";
        var destination = new UriBuilder(baseUri)
        {
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/{destinationPath}"
        };

        if (result.Success)
        {
            destination.Query = $"status=success&txnid={Uri.EscapeDataString(result.TransactionId)}&tier={result.Tier}";
        }
        else
        {
            var error = Uri.EscapeDataString(result.ErrorMessage ?? ApiMessages.PaymentProcessingFailed);
            destination.Query = $"status=failed&error={error}";
        }

        var destinationUri = destination.Uri;
        if (!string.Equals(destinationUri.Scheme, baseUri.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(destinationUri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)
            || destinationUri.Port != baseUri.Port)
        {
            destinationUri = new UriBuilder(baseUri)
            {
                Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/settings/subscription",
                Query = destination.Query
            }.Uri;
        }

        return Redirect(destinationUri.ToString());
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
