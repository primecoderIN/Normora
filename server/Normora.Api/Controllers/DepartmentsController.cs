using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.Departments;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides administrative endpoints for managing organizational departments within a tenant.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireTenant("admin")] // Only admins can manage departments
public class DepartmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of all departments belonging to the current tenant.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
        var result = await mediator.Send(new GetDepartmentsQuery());
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(result));
    }

    /// <summary>
    /// Creates a new department in the current tenant.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var command = new CreateDepartmentCommand(request.Name);
        var id = await mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Department created successfully."));
    }

    /// <summary>
    /// Updates the name of an existing department.
    /// </summary>
    [HttpPut("{id}")]
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
    [HttpDelete("{id}")]
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
