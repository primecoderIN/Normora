using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.Departments;
using Microsoft.AspNetCore.Http;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides administrative endpoints for managing organizational departments within a tenant.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireTenant("admin")] // Only admins can manage departments
[Produces("application/json")]
public class DepartmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of all departments belonging to the current tenant.
    /// </summary>
    /// <returns>A list of departments.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<List<DepartmentDto>>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetDepartments()
    {
        var result = await mediator.Send(new GetDepartmentsQuery());
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(result));
    }

    /// <summary>
    /// Creates a new department in the current tenant.
    /// </summary>
    /// <param name="request">The payload containing the department name.</param>
    /// <returns>The unique identifier of the newly created department.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<Guid>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var command = new CreateDepartmentCommand(request.Name);
        var id = await mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Department created successfully."));
    }

    /// <summary>
    /// Updates the name of an existing department.
    /// </summary>
    /// <param name="id">The unique identifier of the department to update.</param>
    /// <param name="request">The payload containing the new department name.</param>
    /// <returns>A success message if updated.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var command = new UpdateDepartmentCommand(id, request.Name);
        var success = await mediator.Send(command);

        if (!success)
        {
            return NotFound(ApiResponse.Failure("Department not found."));
        }

        return Ok(ApiResponse.Ok("Department updated successfully."));
    }

    /// <summary>
    /// Deletes an existing department.
    /// </summary>
    /// <param name="id">The unique identifier of the department to delete.</param>
    /// <returns>A success message if deleted.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var command = new DeleteDepartmentCommand(id);
        var success = await mediator.Send(command);

        if (!success)
        {
            return NotFound(ApiResponse.Failure("Department not found."));
        }

        return Ok(ApiResponse.Ok("Department deleted successfully."));
    }
}

/// <summary>
/// Request payload for creating a department.
/// </summary>
public record CreateDepartmentRequest(string Name);

/// <summary>
/// Request payload for updating a department.
/// </summary>
public record UpdateDepartmentRequest(string Name);
