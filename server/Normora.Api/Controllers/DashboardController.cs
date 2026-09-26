using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Features.Dashboard;
using Normora.Api.Middleware;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DashboardController(IMediator mediator, ITenantContext tenantContext) : ControllerBase
{

    [HttpGet]
    [RequireTenant(TenantRoles.Admin)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) return Forbid();

        var response = await mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(response);
    }
}
