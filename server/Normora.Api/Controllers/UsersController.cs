using Normora.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.Users;
using Microsoft.AspNetCore.Http;
using Normora.Shared;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves the current authenticated user's profile and their associated tenant memberships.
    /// Used heavily by the frontend router to determine authorization logic.
    /// </summary>
    /// <returns>The authenticated user's profile and their active workspaces.</returns>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<CurrentUserDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetMe()
    {
        var query = new GetCurrentUserQuery();
        var result = await mediator.Send(query);

        return Ok(ApiResponse<CurrentUserDto>.Ok(result));
    }

    /// <summary>
    /// Retrieves a specific user's assigned departments and groups within the current tenant.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user's assigned departments and user groups.</returns>
    [HttpGet("{userId}/assignments")]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserAssignmentsDto>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUserAssignments(Guid userId)
    {
        var result = await mediator.Send(new GetUserAssignmentsQuery(userId));
        if (result == null) return NotFound(ApiResponse.Failure("User not found in this tenant."));
        return Ok(ApiResponse<UserAssignmentsDto>.Ok(result));
    }

    /// <summary>
    /// Updates a user's assigned departments and user groups within the current tenant.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The payload containing the new department and group IDs.</param>
    /// <returns>A success message if the update was successful.</returns>
    [HttpPut("{userId}/assignments")]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
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

