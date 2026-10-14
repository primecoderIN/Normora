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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user's profile and their active workspaces.</returns>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<CurrentUserDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentUserQuery();
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<CurrentUserDto>.Ok(result));
    }

    /// <summary>
    /// Retrieves a specific user's assigned departments and groups within the current tenant.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's assigned departments and user groups.</returns>
    [HttpGet("{userId}/assignments")]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserAssignmentsDto>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUserAssignments(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetUserAssignmentsQuery(userId), cancellationToken);
        if (result == null) return NotFound(ApiResponse.Failure(ApiMessages.NotFound));
        return Ok(ApiResponse<UserAssignmentsDto>.Ok(result));
    }

    /// <summary>
    /// Updates a user's assigned departments and user groups within the current tenant.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The payload containing the new department and group IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success message if the update was successful.</returns>
    [HttpPut("{userId}/assignments")]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserAssignments(Guid userId, [FromBody] UpdateUserAssignmentsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateUserAssignmentsCommand(userId, request.DepartmentIds ?? new List<Guid>(), request.UserGroupIds ?? new List<Guid>());
        await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse.Ok(ApiMessages.UserAssignmentsUpdated));
    }
}

public record UpdateUserAssignmentsRequest(List<Guid>? DepartmentIds, List<Guid>? UserGroupIds);
