using Normora.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.UserGroups;
using Microsoft.AspNetCore.Http;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides administrative endpoints for managing user groups and their department assignments.
/// </summary>
[ApiController]
[Route("api/user-groups")]
[RequireTenant(TenantRoles.Admin)] // Only admins can manage user groups
[Produces("application/json")]
public class UserGroupsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of all user groups within the current tenant, including their assigned departments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of user groups.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<List<UserGroupDto>>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUserGroups(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetUserGroupsQuery(), cancellationToken);
        return Ok(ApiResponse<List<UserGroupDto>>.Ok(result));
    }

    /// <summary>
    /// Retrieves a specific user group's details, including its assignments.
    /// </summary>
    /// <param name="id">The unique identifier of the user group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed user group information.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserGroupDetailDto>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUserGroupById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetUserGroupByIdQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse.Failure(ApiMessages.NotFound));
        }
        return Ok(ApiResponse<UserGroupDetailDto>.Ok(result));
    }

    /// <summary>
    /// Creates a new user group and optionally assigns it to departments.
    /// </summary>
    /// <param name="request">The payload containing the user group name and department assignments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unique identifier of the newly created user group.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<Guid>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroupRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserGroupCommand(request.Name, request.Description, request.DepartmentIds ?? new List<Guid>());
        var id = await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<Guid>.Ok(id, ApiMessages.UserGroupCreated));
    }

    /// <summary>
    /// Updates an existing user group's name and its department assignments.
    /// </summary>
    /// <param name="id">The unique identifier of the user group to update.</param>
    /// <param name="request">The payload containing the updated name and department assignments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success message if updated.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserGroup(Guid id, [FromBody] UpdateUserGroupRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateUserGroupCommand(id, request.Name, request.Description, request.DepartmentIds ?? new List<Guid>());
        await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(ApiMessages.UserGroupUpdated));
    }

    /// <summary>
    /// Updates the member users and department assignments for an existing user group.
    /// </summary>
    /// <param name="id">The unique identifier of the user group to update.</param>
    /// <param name="request">The payload containing the updated assignments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success message if updated.</returns>
    [HttpPut("{id}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserGroupAssignments(Guid id, [FromBody] UpdateUserGroupAssignmentsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateUserGroupAssignmentsCommand(id, request.MemberUserIds ?? new List<Guid>(), request.DepartmentIds ?? new List<Guid>());
        await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(ApiMessages.UserGroupAssignmentsUpdated));
    }

    /// <summary>
    /// Deletes an existing user group.
    /// </summary>
    /// <param name="id">The unique identifier of the user group to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success message if deleted.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> DeleteUserGroup(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserGroupCommand(id);
        await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(ApiMessages.UserGroupDeleted));
    }
}

/// <summary>
/// Request payload for creating a user group.
/// </summary>
public record CreateUserGroupRequest(string Name, string? Description, List<Guid>? DepartmentIds);

/// <summary>
/// Request payload for updating a user group.
/// </summary>
public record UpdateUserGroupRequest(string Name, string? Description, List<Guid>? DepartmentIds);

/// <summary>
/// Request payload for updating a user group's assignments.
/// </summary>
public record UpdateUserGroupAssignmentsRequest(List<Guid>? MemberUserIds, List<Guid>? DepartmentIds);
