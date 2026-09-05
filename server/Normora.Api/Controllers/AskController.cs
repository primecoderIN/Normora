using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Features.Ask;
using Normora.Api.Middleware;
using Normora.Shared;

namespace Normora.Api.Controllers;

[ApiController]
[Route("api/ask")]
[RequireTenant("employee", "admin")]
public sealed class AskController(IMediator mediator) : ControllerBase
{
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

public sealed record AskRequest(string Question, int Limit = 5);