using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Modules.Tenants.Application.Invitations;
using Normora.Shared;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves details about an invitation (tenant name, validity).
    /// This endpoint is anonymous so the accept page can load before the user logs in.
    /// </summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
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
    [HttpPost("{token}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptInvitation(Guid token)
    {
        var command = new AcceptInvitationCommand(token);
        var success = await mediator.Send(command);

        if (!success)
            return BadRequest(ApiResponse.Failure("Failed to accept invitation. It may be expired or invalid."));

        return Ok(ApiResponse.Ok("Invitation accepted successfully."));
    }
}
