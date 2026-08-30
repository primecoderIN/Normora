using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Modules.Tenants.Application.CreateTenant;
using Normora.Modules.Tenants.Application.SuspendTenant;
using Normora.Modules.Tenants.Application.Invitations;
using Normora.Api.Middleware;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Handles HTTP requests for managing Tenants and their lifecycle.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new Tenant. The authenticated user making the request will automatically
    /// be assigned the 'Admin' role for the newly created tenant.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var command = new CreateTenantCommand(request.Name, request.Slug);
        var tenant = await mediator.Send(command);
        return Ok(tenant);
    }

    /// <summary>
    /// Suspends a tenant, preventing its members from accessing tenant resources.
    /// Only users with the 'admin' role within this specific tenant can perform this action.
    /// </summary>
    [HttpPost("{id}/suspend")]
    [RequireTenant("admin")]
    public async Task<IActionResult> SuspendTenant(Guid id)
    {
        var command = new SuspendTenantCommand(id);
        var success = await mediator.Send(command);
        
        if (!success) return NotFound(new { Message = "Tenant not found." });

        return Ok(new { Message = "Tenant suspended successfully." });
    }

    /// <summary>
    /// Invites a new user to the tenant via email.
    /// Only users with the 'admin' role within this specific tenant can perform this action.
    /// </summary>
    [HttpPost("invitations")]
    [RequireTenant("admin")]
    public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeRequest request)
    {
        var command = new InviteEmployeeCommand(request.Email);
        var token = await mediator.Send(command);

        return Ok(ApiResponse<Guid>.Ok(token, "Invitation sent successfully."));
    }
}

public record InviteEmployeeRequest(string Email);

public record CreateTenantRequest(string Name, string Slug);
