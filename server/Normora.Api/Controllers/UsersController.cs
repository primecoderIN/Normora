using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
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

    [HttpGet("{userId}/assignments")]
    [RequireTenant("admin")]
    public async Task<IActionResult> GetUserAssignments(Guid userId)
    {
        var result = await mediator.Send(new GetUserAssignmentsQuery(userId));
        if (result == null) return NotFound(ApiResponse.Failure("User not found in this tenant."));
        return Ok(ApiResponse<UserAssignmentsDto>.Ok(result));
    }

    [HttpPut("{userId}/assignments")]
    [RequireTenant("admin")]
    public async Task<IActionResult> UpdateUserAssignments(Guid userId, [FromBody] UpdateUserAssignmentsRequest request)
    {
        var command = new UpdateUserAssignmentsCommand(userId, request.DepartmentIds ?? new List<Guid>(), request.UserGroupIds ?? new List<Guid>());
        var success = await mediator.Send(command);

        if (!success)
        {
            return NotFound(ApiResponse.Failure("User not found in this tenant."));
        }

        return Ok(ApiResponse.Ok("User assignments updated successfully."));
    }
}

public record UpdateUserAssignmentsRequest(List<Guid>? DepartmentIds, List<Guid>? UserGroupIds);
