using Normora.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Shared.Authorization;
using Normora.Modules.Tenants.Application.Departments.Commands;
using Normora.Modules.Tenants.Application.Departments.DTOs;
using Normora.Modules.Tenants.Application.Departments.Queries;
using Microsoft.AspNetCore.Http;
using Normora.Shared;

namespace Normora.Modules.Tenants.API;

/// <summary>
/// Provides administrative endpoints for managing organizational departments within a tenant.
/// </summary>
// Decentralized Controller Architecture:
// In Normora's modular monolith, controllers reside directly in their owning module (Modules/Tenants/API/).
// This controller handles CRUD operations for organizational departments.
// Security:
// 1. [Authorize] ensures an authenticated OIDC/BFF session.
// 2. [RequireTenant(TenantRoles.Admin)] enforces BFLA (Broken Function Level Authorization),
//    verifying the user has the Admin role within the current ambient tenant workspace.
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireTenant(TenantRoles.Admin)]
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
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken = default)
    {
        // MediatR Query Dispatch:
        // Dispatches to GetDepartmentsQueryHandler which reads tenant departments with AsNoTracking.
        var result = await mediator.Send(new GetDepartmentsQuery(), cancellationToken);
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(result));
    }

    /// <summary>
    /// Creates a new department in the current tenant.
    /// </summary>
    /// <param name="request">The payload containing the department name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unique identifier of the newly created department.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<Guid>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        // CQRS Command Dispatch:
        // Creates a new organizational department after verifying unique naming per tenant.
        var command = new CreateDepartmentCommand(request.Name, request.Description);
        var id = await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<Guid>.Ok(id, ApiMessages.DepartmentCreated));
    }

    /// <summary>
    /// Updates the name of an existing department.
    /// </summary>
    /// <param name="id">The unique identifier of the department to update.</param>
    /// <param name="request">The payload containing the new department name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success message if updated.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        // BOLA-Protected Update:
        // UpdateDepartmentCommandHandler verifies the department exists and belongs to the active tenant.
        var command = new UpdateDepartmentCommand(id, request.Name, request.Description);
        await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(ApiMessages.DepartmentUpdated));
    }

    /// <summary>
    /// Deletes an existing department.
    /// </summary>
    /// <param name="id">The unique identifier of the department to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success message if deleted.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken = default)
    {
        // BOLA-Protected Deletion:
        // Cascades removal to employee department mappings and user group links.
        var command = new DeleteDepartmentCommand(id);
        await mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(ApiMessages.DepartmentDeleted));
    }
}

/// <summary>
/// Request payload for creating a department.
/// </summary>
public record CreateDepartmentRequest(string Name, string? Description = null);

/// <summary>
/// Request payload for updating a department.
/// </summary>
public record UpdateDepartmentRequest(string Name, string? Description = null);
