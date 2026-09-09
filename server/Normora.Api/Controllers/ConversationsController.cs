using MediatR;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Conversations.Application.Commands;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Application.Queries;
using Normora.Shared;

namespace Normora.Modules.Conversations.Controllers;

[ApiController]
[Route("api/conversations")]
[RequireTenant("employee", "admin")]
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
}
