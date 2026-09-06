using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.UserGroups;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides administrative endpoints for managing user groups and their department assignments.
/// </summary>
[ApiController]
[Route("api/user-groups")]
[RequireTenant("admin")] // Only admins can manage user groups
public class UserGroupsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of all user groups within the current tenant, including their assigned departments.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserGroups()
    {
        var result = await mediator.Send(new GetUserGroupsQuery());
        return Ok(ApiResponse<List<UserGroupDto>>.Ok(result));
    }

    /// <summary>
    /// Creates a new user group and optionally assigns it to departments.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroupRequest request)
    {
        var command = new CreateUserGroupCommand(request.Name, request.DepartmentIds ?? new List<Guid>());
        var id = await mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "User group created successfully."));
    }

    /// <summary>
    /// Updates an existing user group's name and its department assignments.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUserGroup(Guid id, [FromBody] UpdateUserGroupRequest request)
    {
        var command = new UpdateUserGroupCommand(id, request.Name, request.DepartmentIds ?? new List<Guid>());
        var success = await mediator.Send(command);

        if (!success)
        {
            return NotFound(ApiResponse.Failure("User group not found."));
        }

        return Ok(ApiResponse.Ok("User group updated successfully."));
    }

    /// <summary>
    /// Deletes an existing user group.
    /// </summary>
    [HttpDelete("{id}")]
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
public record CreateUserGroupRequest(string Name, List<Guid>? DepartmentIds);

/// <summary>
/// Request payload for updating a user group.
/// </summary>
public record UpdateUserGroupRequest(string Name, List<Guid>? DepartmentIds);
