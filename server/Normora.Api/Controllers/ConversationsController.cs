using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Normora.Api.Features.Ask;
using Normora.Api.Middleware;
using Normora.Modules.Conversations.Application.Commands;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Application.Queries;
using Normora.Shared;

namespace Normora.Modules.Conversations.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireTenant("employee", "admin")]
[EnableRateLimiting("ai")]
public class ConversationsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateConversation()
    {
        var result = await mediator.Send(new CreateConversationCommand());
        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var result = await mediator.Send(new GetConversationsQuery(limit, offset));
        return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var result = await mediator.Send(new GetConversationQuery(id));
        if (result is null)
        {
            return NotFound(ApiResponse.Failure("Conversation not found."));
        }
        return Ok(ApiResponse<ConversationDetailDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        await mediator.Send(new DeleteConversationCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Sends a message to a conversation and receives a grounded AI answer.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AskConversation(Guid id, [FromBody] AskConversationRequest body)
    {
        var result = await mediator.Send(new AskConversationCommand(id, body.Question, body.Limit));
        return Ok(ApiResponse<AskConversationResult>.Ok(result));
    }

    /// <summary>
    /// Sends a message to a conversation and receives a grounded AI answer via Server-Sent Events (SSE).
    /// </summary>
    [HttpPost("{id:guid}/messages/stream")]
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
