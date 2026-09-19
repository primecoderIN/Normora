using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Normora.Api.Features.Ask;
using Normora.Api.Middleware;
using Normora.Modules.Conversations.Application.Commands;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Application.Queries;
using Microsoft.AspNetCore.Http;
using Normora.Shared;

namespace Normora.Modules.Conversations.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireTenant("employee", "admin")]
[EnableRateLimiting("ai")]
[Produces("application/json")]
public class ConversationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new, empty conversation shell.
    /// </summary>
    /// <returns>The newly created conversation.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ConversationDto>))]
    public async Task<IActionResult> CreateConversation()
    {
        var result = await mediator.Send(new CreateConversationCommand());
        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    /// <summary>
    /// Lists all conversations for the authenticated user in the current tenant.
    /// </summary>
    /// <param name="limit">The maximum number of conversations to return.</param>
    /// <param name="offset">The number of conversations to skip.</param>
    /// <returns>A paginated list of conversations.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IReadOnlyList<ConversationDto>>))]
    public async Task<IActionResult> GetConversations([FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var result = await mediator.Send(new GetConversationsQuery(limit, offset));
        return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Ok(result));
    }

    /// <summary>
    /// Retrieves full details of a specific conversation including all historical messages.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation.</param>
    /// <returns>The conversation details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ConversationDetailDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var result = await mediator.Send(new GetConversationQuery(id));
        if (result is null)
        {
            return NotFound(ApiResponse.Failure("Conversation not found."));
        }
        return Ok(ApiResponse<ConversationDetailDto>.Ok(result));
    }

    /// <summary>
    /// Permanently deletes a conversation and all of its messages.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        await mediator.Send(new DeleteConversationCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Sends a message to a conversation and receives a grounded AI answer.
    /// </summary>
    /// <param name="id">The unique identifier of the conversation.</param>
    /// <param name="body">The payload containing the user's question.</param>
    /// <returns>The AI's answer along with citations.</returns>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<AskConversationResult>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    public async Task<IActionResult> AskConversation(Guid id, [FromBody] AskConversationRequest body)
    {
        var result = await mediator.Send(new AskConversationCommand(id, body.Question, body.Limit));
        return Ok(ApiResponse<AskConversationResult>.Ok(result));
    }

    /// <summary>
    /// Sends a message to a conversation and receives a grounded AI answer via Server-Sent Events (SSE).
    /// </summary>
    /// <param name="id">The unique identifier of the conversation.</param>
    /// <param name="body">The payload containing the user's question.</param>
    /// <returns>A stream of text chunks representing the AI's answer.</returns>
    [HttpPost("{id:guid}/messages/stream")]
    [Produces("text/event-stream")]
    public async Task AskConversationStream(Guid id, [FromBody] AskConversationRequest body)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var stream = mediator.CreateStream(new AskConversationStreamCommand(id, body.Question, body.Limit));

        await foreach (var evt in stream)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(evt, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}

public sealed record AskConversationRequest(string Question, int Limit = 5);
