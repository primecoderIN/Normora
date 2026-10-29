using Normora.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Normora.Modules.Documents.Application.Documents;
using Normora.Modules.Documents.Application.Chunks;


using Normora.Shared;
using Normora.Shared.Contracts.Tenants;
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
public class DocumentsController(IMediator mediator, ITenantContext tenantContext, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Retrieves all documents for the currently authenticated tenant.
    /// Uses MediatR to send a GetEmployerDocumentsQuery. Data isolation is handled at the DbContext level.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of documents for the current tenant.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<List<DocumentDto>>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken = default)
    {
        var query = new GetEmployerDocumentsQuery();
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<List<DocumentDto>>.Ok(result));
    }

    /// <summary>
    /// Searches documents using vector similarity for the current tenant.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of search results with relevance scores.</returns>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IReadOnlyList<DocumentSearchResult>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    public async Task<IActionResult> SearchDocuments([FromQuery] string query, [FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse.Failure(ApiMessages.SearchQueryRequired));
        }

        var result = await mediator.Send(new SearchDocumentsQuery(query.Trim(), limit), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DocumentSearchResult>>.Ok(result));
    }

    /// <summary>
    /// Retrieves the content of a specific document chunk. Used for citation previews.
    /// Access is restricted to chunks that belong to documents visible to the current user.
    /// </summary>
    /// <param name="chunkId">The unique chunk identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("chunks/{chunkId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<DocumentChunkPreviewDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetDocumentChunk(Guid chunkId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetDocumentChunkQuery(chunkId), cancellationToken);
        return Ok(ApiResponse<DocumentChunkPreviewDto>.Ok(result));
    }

    /// <summary>
    /// Uploads a new document file and metadata.
    /// Limits payload size to 100MB.
    /// </summary>
    /// <param name="file">The physical file to upload.</param>
    /// <param name="departmentIds">Optional department IDs to assign to the document.</param>
    /// <param name="documentId">Optional document container ID to append a new version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created document record.</returns>
    [HttpPost("upload")]
    [RequireTenant(TenantRoles.Admin)] // Security: only admins may add documents to the knowledge base
    [RequestSizeLimit(100_971_520)] // 100 MB max payload size for ASP.NET
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<DocumentDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    public async Task<IActionResult> UploadDocument(
        [FromForm] IFormFile file, 
        [FromForm] Guid[]? departmentIds = null, 
        [FromForm] Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var command = new UploadDocumentCommand(file, tenantContext.TenantId.Value, departmentIds, documentId);
        var document = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<DocumentDto>.Ok(document, ApiMessages.DocumentUploaded));
    }

    /// <summary>
    /// Permanently deletes a document from the system and storage.
    /// </summary>
    /// <param name="id">The unique identifier of the document to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success message if deleted.</returns>
    [HttpDelete("{id}")]
    [RequireTenant(TenantRoles.Admin)] // Security: only admins may delete documents from the knowledge base
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken = default)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var keycloakId = currentUser.ExternalUserId;
        var userId = await mediator.Send(new GetUserIdQuery(keycloakId), cancellationToken);
        var deletedByUserId = userId ?? Guid.Empty;

        var command = new DeleteDocumentCommand(id, tenantContext.TenantId.Value, deletedByUserId);
        await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse.Ok(ApiMessages.DocumentDeleted));
    }
}
