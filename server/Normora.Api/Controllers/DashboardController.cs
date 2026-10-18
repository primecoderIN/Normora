using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Features.Dashboard;
using Normora.Api.Middleware;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Exposes administrative metrics and high-level tenant statistics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DashboardController(IMediator mediator, ITenantContext tenantContext) : ControllerBase
{
    /// <summary>
    /// Retrieves tenant dashboard summary including document counts, pending invites, and active members.
    /// Restricted to Tenant Administrators.
    /// </summary>
    [HttpGet]
    [RequireTenant(TenantRoles.Admin)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        // Enforce resolved tenant context
        if (!tenantContext.TenantId.HasValue) 
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failure(Normora.Shared.Constants.ApiMessages.Forbidden));

        var response = await mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(response));
    }
}

