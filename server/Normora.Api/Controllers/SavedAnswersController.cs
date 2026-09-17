using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Conversations.Application.Commands;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Application.Queries;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides endpoints for employees to save, unsave, and list bookmarked AI answers.
/// </summary>
[ApiController]
[Route("api/saved-answers")]
[RequireTenant("employee", "admin")]
public class SavedAnswersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns all saved answers for the current user, ordered newest-first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSavedAnswers(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var result = await mediator.Send(new GetSavedAnswersQuery(limit, offset));
        return Ok(ApiResponse<IReadOnlyList<SavedAnswerDto>>.Ok(result));
    }

    /// <summary>
    /// Saves an assistant message as a bookmark. Idempotent.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerRequest request)
    {
        var id = await mediator.Send(new SaveAnswerCommand(request.MessageId));
        return Ok(ApiResponse<Guid>.Ok(id, "Answer saved."));
    }

    /// <summary>
    /// Removes a saved answer by the original message ID. Idempotent.
    /// </summary>
    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> UnsaveAnswer(Guid messageId)
    {
        await mediator.Send(new UnsaveAnswerCommand(messageId));
        return NoContent();
    }
}

/// <summary>Request payload for saving an answer.</summary>
public record SaveAnswerRequest(Guid MessageId);
