using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.Audit;
using Normora.Shared;
using Normora.Shared.Constants;
using Normora.Shared.Models;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides read-only access to the tenant's tamper-evident audit trail for workspace administrators.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "employer")]
[Produces("application/json")]
public class AuditLogsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated and filterable list of audit log records for the active tenant.
    /// </summary>
    [HttpGet]
    [RequireTenant]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<PagedResult<AuditLogDto>>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(page, pageSize, action, search, from, to), cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result));
    }
}
