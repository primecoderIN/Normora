using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Modules.Tenants.Application.Dashboard.DTOs;
using Normora.Modules.Tenants.Application.Dashboard.Queries;
using Normora.Shared.Authorization;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared;

namespace Normora.Modules.Tenants.API;

/// <summary>
/// Exposes administrative metrics and high-level tenant statistics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves tenant dashboard summary including document counts, pending invites, and active members.
    /// Restricted to Tenant Administrators.
    /// Supports both GET /api/dashboard and GET /api/dashboard/summary.
    /// </summary>
    [HttpGet]
    [HttpGet("summary")]
    [RequireTenant(TenantRoles.Admin)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(response));
    }
}

