using Normora.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Conversations.Application.Commands;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Application.Queries;
using Microsoft.AspNetCore.Http;
using Normora.Shared;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides endpoints for employees to save, unsave, and list bookmarked AI answers.
/// </summary>
[ApiController]
[Route("api/saved-answers")]
[RequireTenant(TenantRoles.Employee, TenantRoles.Admin)]
[Produces("application/json")]
public class SavedAnswersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns all saved answers for the current user, ordered newest-first.
    /// </summary>
    /// <summary>
    /// Returns all saved answers for the current user, ordered newest-first.
    /// </summary>
    /// <param name="limit">The maximum number of saved answers to return.</param>
    /// <param name="offset">The number of saved answers to skip.</param>
    /// <returns>A paginated list of saved answers.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IReadOnlyList<SavedAnswerDto>>))]
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
    /// <summary>
    /// Saves an assistant message as a bookmark. Idempotent.
    /// </summary>
    /// <param name="request">The payload containing the ID of the message to save.</param>
    /// <returns>The unique identifier of the saved answer bookmark.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<Guid>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerRequest request)
    {
        var id = await mediator.Send(new SaveAnswerCommand(request.MessageId));
        return Ok(ApiResponse<Guid>.Ok(id, "Answer saved."));
    }

    /// <summary>
    /// Removes a saved answer by the original message ID. Idempotent.
    /// </summary>
    /// <summary>
    /// Removes a saved answer by the original message ID. Idempotent.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message bookmark to remove.</param>
    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveAnswer(Guid messageId)
    {
        await mediator.Send(new UnsaveAnswerCommand(messageId));
        return NoContent();
    }
}

/// <summary>Request payload for saving an answer.</summary>
public record SaveAnswerRequest(Guid MessageId);

