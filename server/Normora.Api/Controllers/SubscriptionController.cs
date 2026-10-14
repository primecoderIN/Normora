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

/// <summary>
/// Handles subscription plan limits, quota metrics, and tier management.
/// </summary>
[ApiController]
[Route("api/subscription")]
[Authorize]
[RequireTenant(TenantRoles.Admin)]
[Produces("application/json")]
public class SubscriptionController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves current resource usage and subscription boundaries for the active tenant.
    /// </summary>
    [HttpGet("usage")]
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
    /// Updates or upgrades the subscription plan tier for the active tenant.
    /// </summary>
    [HttpPost("tier")]
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

