using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Modules.Tenants.Application.Users;
using Normora.Shared;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves the current authenticated user's profile and their associated tenant memberships.
    /// Used heavily by the frontend router to determine authorization logic.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var query = new GetCurrentUserQuery();
        var result = await mediator.Send(query);

        return Ok(ApiResponse<CurrentUserDto>.Ok(result));
    }
}
