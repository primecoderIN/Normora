using Normora.Shared.Constants;
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
    /// Updates the active tenant's white-label branding configuration.
    /// </summary>
    [HttpPut("branding")]
    [RequireTenant(TenantRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<TenantBrandingDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UpdateTenantBranding(
        [FromForm] string? primaryColor,
        [FromForm] string? secondaryColor,
        [FromForm] IFormFile? logoFile,
        [FromForm] IFormFile? logoFileDark,
        [FromForm] IFormFile? faviconFile)
    {
        if (!tenantContext.TenantId.HasValue)
            return Forbid();

        var command = new UpdateTenantBrandingCommand(
            tenantContext.TenantId.Value,
            primaryColor,
            secondaryColor,
            logoFile,
            logoFileDark,
            faviconFile
        );

        var result = await mediator.Send(command);

        if (result == null)
            return NotFound(ApiResponse.Failure("Tenant not found."));

        return Ok(ApiResponse<TenantBrandingDto>.Ok(result));
    }

    /// <summary>
    /// Serves a branding asset (logo or favicon) for a given tenant slug.
    /// Allows the frontend to load the image anonymously without MinIO pre-signed URLs.
    /// </summary>
    [HttpGet("branding/{slug}/{assetType}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)] // Cache for 24 hours
    public async Task<IActionResult> GetBrandingAsset(
        string slug, 
        string assetType, 
        [FromServices] Normora.Modules.Tenants.Persistence.IBrandingStorageService storageService)
    {
        if (assetType != "logo" && assetType != "logo-dark" && assetType != "favicon")
            return BadRequest();

        try
        {
            var objectName = $"{slug}/{assetType}";
            var (stream, contentType) = await storageService.DownloadAssetAsync(objectName);
            return File(stream, contentType);
        }
        catch
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Suspends a tenant, preventing its members from accessing tenant resources.
    /// Only users with the 'admin' role within this specific tenant can perform this action.
    /// </summary>
    /// <param name="id">The unique identifier of the tenant to suspend.</param>
    /// <returns>A success message if the suspension was successful.</returns>
    [HttpPost("{id}/suspend")]
    [RequireTenant(TenantRoles.Admin)]
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
    [RequireTenant(TenantRoles.Admin)]
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

