using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Features.Documents;
using Normora.Shared;
using Normora.Shared.Interfaces;
using Normora.Api.Middleware;
using System.Security.Claims;

namespace Normora.Api.Controllers;

/// <summary>
/// Handles HTTP requests related to Document management (uploading, retrieving, deleting).
/// Only authenticated users who are part of a Tenant (with 'admin' or 'employee' roles) can access these endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireTenant("admin", "employee")]
public class DocumentsController(IMediator mediator, ITenantContext tenantContext) : ControllerBase
{

    /// <summary>
    /// Retrieves all documents for the currently authenticated tenant.
    /// Uses MediatR to send a GetEmployerDocumentsQuery. Data isolation is handled at the DbContext level.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        var query = new GetEmployerDocumentsQuery();
        var result = await mediator.Send(query);

        return Ok(ApiResponse<List<Document>>.Ok(result));
    }

    /// <summary>
    /// Uploads a new document file and metadata.
    /// Limits payload size to 100MB.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100_971_520)] // 100 MB max payload size for ASP.NET
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file)
    {
        // 1. Ensure the user is acting within a valid Tenant context.
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue) 
            return Unauthorized(ApiResponse.Failure("Tenant context not found."));

        // 2. Validation (file empty, size, extension) is now handled automatically
        // by FluentValidation through the MediatR Pipeline Behavior.
        var command = new UploadDocumentCommand(file, tenantContext.TenantId.Value);
        
        // 3. Dispatch the command to the MediatR handler.
        var document = await mediator.Send(command);

        return Ok(ApiResponse<Document>.Ok(document, "Document uploaded successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue) 
            return Unauthorized(ApiResponse.Failure("Tenant context not found."));

        var command = new DeleteDocumentCommand(id, tenantContext.TenantId.Value);
        var deleted = await mediator.Send(command);

        if (!deleted) return NotFound(ApiResponse.Failure("Document not found."));

        return Ok(ApiResponse.Ok("Document deleted successfully."));
    }
}
