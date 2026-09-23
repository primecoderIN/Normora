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
    /// <returns>A list of user groups.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<List<UserGroupDto>>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUserGroups()
    {
        var result = await mediator.Send(new GetUserGroupsQuery());
        return Ok(ApiResponse<List<UserGroupDto>>.Ok(result));
    }

    /// <summary>
    /// Retrieves a specific user group's details, including its assignments.
    /// </summary>
    /// <param name="id">The unique identifier of the user group.</param>
    /// <returns>Detailed user group information.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserGroupDetailDto>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetUserGroupById(Guid id)
    {
        var result = await mediator.Send(new GetUserGroupByIdQuery(id));
        if (result == null)
        {
            return NotFound(ApiResponse.Failure("User group not found."));
        }
        return Ok(ApiResponse<UserGroupDetailDto>.Ok(result));
    }

    /// <summary>
    /// Creates a new user group and optionally assigns it to departments.
    /// </summary>
    /// <param name="request">The payload containing the user group name and department assignments.</param>
    /// <returns>The unique identifier of the newly created user group.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<Guid>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroupRequest request)
    {
        var command = new CreateUserGroupCommand(request.Name, request.Description, request.DepartmentIds ?? new List<Guid>());
        var id = await mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "User group created successfully."));
    }

    /// <summary>
    /// Updates an existing user group's name and its department assignments.
    /// </summary>
    /// <param name="id">The unique identifier of the user group to update.</param>
    /// <param name="request">The payload containing the updated name and department assignments.</param>
    /// <returns>A success message if updated.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserGroup(Guid id, [FromBody] UpdateUserGroupRequest request)
    {
        var command = new UpdateUserGroupCommand(id, request.Name, request.Description, request.DepartmentIds ?? new List<Guid>());
        var success = await mediator.Send(command);

        if (!success)
        {
            return NotFound(ApiResponse.Failure("User group not found."));
        }

        return Ok(ApiResponse.Ok("User group updated successfully."));
    }

    /// <summary>
    /// Updates the member users and department assignments for an existing user group.
    /// </summary>
    /// <param name="id">The unique identifier of the user group to update.</param>
    /// <param name="request">The payload containing the updated assignments.</param>
    /// <returns>A success message if updated.</returns>
    [HttpPut("{id}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserGroupAssignments(Guid id, [FromBody] UpdateUserGroupAssignmentsRequest request)
    {
        var command = new UpdateUserGroupAssignmentsCommand(id, request.MemberUserIds ?? new List<Guid>(), request.DepartmentIds ?? new List<Guid>());
        var success = await mediator.Send(command);

        if (!success)
        {
            return NotFound(ApiResponse.Failure("User group not found."));
        }

        return Ok(ApiResponse.Ok("User group assignments updated successfully."));
    }

    /// <summary>
    /// Deletes an existing user group.
    /// </summary>
    /// <param name="id">The unique identifier of the user group to delete.</param>
    /// <returns>A success message if deleted.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> DeleteUserGroup(Guid id)
    {
        var command = new DeleteUserGroupCommand(id);
        var success = await mediator.Send(command);

        if (!success)
        {
            return NotFound(ApiResponse.Failure("User group not found."));
        }

        return Ok(ApiResponse.Ok("User group deleted successfully."));
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
