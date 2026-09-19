using Normora.Shared.Constants;
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
[RequireTenant(TenantRoles.Admin, TenantRoles.Employee)]
[Produces("application/json")]
public class DocumentsController(IMediator mediator, ITenantContext tenantContext) : ControllerBase
{

    /// <summary>
    /// Retrieves all documents for the currently authenticated tenant.
    /// Uses MediatR to send a GetEmployerDocumentsQuery. Data isolation is handled at the DbContext level.
    /// </summary>
    /// <returns>A list of documents for the current tenant.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<List<DocumentDto>>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetDocuments()
    {
        var query = new GetEmployerDocumentsQuery();
        var result = await mediator.Send(query);

        return Ok(ApiResponse<List<DocumentDto>>.Ok(result));
    }

    /// <summary>
    /// Searches documents using vector similarity for the current tenant.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>A list of search results with relevance scores.</returns>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IReadOnlyList<DocumentSearchResult>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    public async Task<IActionResult> SearchDocuments([FromQuery] string query, [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse.Failure("A search query is required."));
        }

        var result = await mediator.Send(new SearchDocumentsQuery(query.Trim(), limit));
        return Ok(ApiResponse<IReadOnlyList<DocumentSearchResult>>.Ok(result));
    }

    /// <summary>
    /// Uploads a new document file and metadata.
    /// Limits payload size to 100MB.
    /// </summary>
    /// <param name="file">The physical file to upload.</param>
    /// <param name="departmentIds">Optional department IDs to assign to the document.</param>
    /// <returns>The newly created document record.</returns>
    [HttpPost("upload")]
    [RequireTenant(TenantRoles.Admin)] // Security: only admins may add documents to the knowledge base
    [RequestSizeLimit(100_971_520)] // 100 MB max payload size for ASP.NET
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<DocumentDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] Guid[]? departmentIds = null)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        // 1. Validation (file empty, size, extension) is handled automatically
        // by FluentValidation through the MediatR Pipeline Behavior.
        var command = new UploadDocumentCommand(file, tenantContext.TenantId.Value, departmentIds);
        
        // 3. Dispatch the command to the MediatR handler.
        var document = await mediator.Send(command);

        return Ok(ApiResponse<DocumentDto>.Ok(document, "Document uploaded successfully."));
    }

    /// <summary>
    /// Permanently deletes a document from the system and storage.
    /// </summary>
    /// <param name="id">The unique identifier of the document to delete.</param>
    /// <returns>A success message if deleted.</returns>
    [HttpDelete("{id}")]
    [RequireTenant(TenantRoles.Admin)] // Security: only admins may delete documents from the knowledge base
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var command = new DeleteDocumentCommand(id, tenantContext.TenantId.Value);
        var deleted = await mediator.Send(command);

        if (!deleted) return NotFound(ApiResponse.Failure("Document not found."));

        return Ok(ApiResponse.Ok("Document deleted successfully."));
    }
}

