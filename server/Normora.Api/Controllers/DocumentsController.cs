using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Features.Documents;
using Normora.Shared;
using System.Security.Claims;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "employer")]
public class DocumentsController(IMediator mediator) : ControllerBase
{
    private string GetEmployerId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        var query = new GetEmployerDocumentsQuery();
        var result = await mediator.Send(query);

        return Ok(ApiResponse<List<Document>>.Ok(result));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_971_520)] // 100 MB max payload size for ASP.NET
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file)
    {
        var employerId = GetEmployerId();
        if (string.IsNullOrEmpty(employerId)) return Unauthorized(ApiResponse.Failure("Unauthorized access."));

        // Validation (file empty, size, extension) is now handled automatically
        // by FluentValidation through the MediatR Pipeline Behavior.
        var command = new UploadDocumentCommand(file, employerId);
        var document = await mediator.Send(command);

        return Ok(ApiResponse<Document>.Ok(document, "Document uploaded successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var employerId = GetEmployerId();
        if (string.IsNullOrEmpty(employerId)) return Unauthorized(ApiResponse.Failure("Unauthorized access."));

        var command = new DeleteDocumentCommand(id, employerId);
        var deleted = await mediator.Send(command);

        if (!deleted) return NotFound(ApiResponse.Failure("Document not found."));

        return Ok(ApiResponse.Ok("Document deleted successfully."));
    }
}
