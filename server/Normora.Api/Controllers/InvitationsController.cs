using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Normora.Modules.Tenants.Application.Invitations;
using Microsoft.AspNetCore.Http;
using Normora.Shared;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InvitationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves details about an invitation (tenant name, validity).
    /// This endpoint is anonymous so the accept page can load before the user logs in.
    /// </summary>
    /// <param name="token">The unique invitation token.</param>
    /// <returns>Details about the invitation such as the tenant name.</returns>
    [HttpGet("{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("anon")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<InvitationDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetInvitation(Guid token)
    {
        var query = new GetInvitationQuery(token);
        var result = await mediator.Send(query);

        if (result == null)
            return NotFound(ApiResponse.Failure("Invitation not found or invalid."));

        return Ok(ApiResponse<InvitationDto>.Ok(result));
    }

    /// <summary>
    /// Accepts the invitation and creates a tenant membership for the authenticated user.
    /// </summary>
    /// <param name="token">The unique invitation token to accept.</param>
    /// <returns>A success message if the invitation was accepted.</returns>
    [HttpPost("{token}/accept")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    public async Task<IActionResult> AcceptInvitation(Guid token)
    {
        var command = new AcceptInvitationCommand(token);
        var success = await mediator.Send(command);

        if (!success)
            return BadRequest(ApiResponse.Failure("Failed to accept invitation. It may be expired or invalid."));

        return Ok(ApiResponse.Ok("Invitation accepted successfully."));
    }
}
