using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Tenants.Application.Departments;
using Normora.Shared;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireTenant("admin")] // Only admins can manage departments
public class DepartmentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
        var result = await mediator.Send(new GetDepartmentsQuery());
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var command = new CreateDepartmentCommand(request.Name);
        var id = await mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Department created successfully."));
    }

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

public record CreateDepartmentRequest(string Name);
public record UpdateDepartmentRequest(string Name);
