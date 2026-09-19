using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normora.Modules.Tenants.Application.CreateTenant;
using Normora.Modules.Tenants.Application.SuspendTenant;
using Normora.Modules.Tenants.Application.Invitations;
using Normora.Modules.Tenants.Application.Branding;
using Normora.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Normora.Shared;
using Normora.Shared.Interfaces;
namespace Normora.Api.Controllers;

/// <summary>
/// Handles HTTP requests for managing Tenants and their lifecycle.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TenantsController(IMediator mediator, ITenantContext tenantContext) : ControllerBase
{
    /// <summary>
    /// Creates a new Tenant. The authenticated user making the request will automatically
    /// be assigned the 'Admin' role for the newly created tenant.
    /// </summary>
    /// <param name="request">The payload containing the new tenant's name and slug.</param>
    /// <returns>The unique identifier of the newly created tenant.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var command = new CreateTenantCommand(request.Name, request.Slug);
        var tenant = await mediator.Send(command);
        return Ok(tenant);
    }

    /// <summary>
    /// Retrieves white-label branding configuration for a tenant by its slug.
    /// Anonymous — called by the frontend before login to skin the app.
    /// </summary>
    /// <param name="slug">The unique URL slug of the tenant.</param>
    /// <returns>The branding configuration for the tenant.</returns>
    [HttpGet("branding/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<TenantBrandingDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetTenantBranding(string slug)
    {
        var query = new GetTenantBrandingQuery(slug);
        var result = await mediator.Send(query);

        if (result == null)
            return NotFound(ApiResponse.Failure("Tenant branding not found."));

        return Ok(ApiResponse<TenantBrandingDto>.Ok(result));
    }

    /// <summary>
    /// Suspends a tenant, preventing its members from accessing tenant resources.
    /// Only users with the 'admin' role within this specific tenant can perform this action.
    /// </summary>
    /// <param name="id">The unique identifier of the tenant to suspend.</param>
    /// <returns>A success message if the suspension was successful.</returns>
    [HttpPost("{id}/suspend")]
    [RequireTenant("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> SuspendTenant(Guid id)
    {
        if (id != tenantContext.TenantId)
        {
            return Forbid();
        }

        var command = new SuspendTenantCommand(id);
        var success = await mediator.Send(command);
        
        if (!success) return NotFound(new { Message = "Tenant not found." });

        return Ok(new { Message = "Tenant suspended successfully." });
    }

    /// <summary>
    /// Invites a new user to the tenant via email.
    /// Only users with the 'admin' role within this specific tenant can perform this action.
    /// </summary>
    /// <param name="request">The payload containing the email address of the user to invite.</param>
    /// <returns>The invitation token that was generated.</returns>
    [HttpPost("invitations")]
    [RequireTenant("admin")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<Guid>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeRequest request)
    {
        var command = new InviteEmployeeCommand(request.Email);
        var token = await mediator.Send(command);

        return Ok(ApiResponse<Guid>.Ok(token, "Invitation sent successfully."));
    }
}

public record InviteEmployeeRequest(string Email);

public record CreateTenantRequest(string Name, string Slug);
