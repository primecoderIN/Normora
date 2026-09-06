using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.UserGroups;
using Normora.Shared;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/user-groups")]
[RequireTenant("admin")] // Only admins can manage user groups
public class UserGroupsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUserGroups()
    {
        var result = await mediator.Send(new GetUserGroupsQuery());
        return Ok(ApiResponse<List<UserGroupDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroupRequest request)
    {
        var command = new CreateUserGroupCommand(request.Name, request.DepartmentIds ?? new List<Guid>());
        var id = await mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "User group created successfully."));
    }

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

public record CreateUserGroupRequest(string Name, List<Guid>? DepartmentIds);
public record UpdateUserGroupRequest(string Name, List<Guid>? DepartmentIds);
