using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Features.Ask;
using Normora.Api.Middleware;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Handles employee requests for the Ask Normora feature (Retrieval-Augmented Generation).
/// </summary>
[ApiController]
[Route("api/ask")]
[RequireTenant("employee", "admin")]
public sealed class AskController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Processes an employee's question and returns a grounded answer derived from the tenant's documents.
    /// </summary>
    /// <param name="request">The question payload.</param>
    /// <returns>A generated answer along with the source citations.</returns>
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(ApiResponse.Failure("A question is required."));
        }

        var result = await mediator.Send(new AskQuestionQuery(request.Question.Trim(), request.Limit));
        return Ok(ApiResponse<AskQuestionResult>.Ok(result));
    }
}

/// <summary>
/// Represents the incoming request for Ask Normora.
/// </summary>
/// <param name="Question">The employee's question.</param>
/// <param name="Limit">The maximum number of document chunks to retrieve and use as context (default 5).</param>
public sealed record AskRequest(string Question, int Limit = 5);