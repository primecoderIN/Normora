using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Normora.Api.Middleware;
using Normora.Modules.Conversations.Application.Queries;
using Normora.Shared;
using Normora.Shared.Constants;

namespace Normora.Api.Controllers;

/// <summary>
/// Provides AI-generated question suggestions to pre-populate the empty-state
/// of the employee conversation interface.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireTenant(TenantRoles.Employee, TenantRoles.Admin)]
[Produces("application/json")]
public class SuggestionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns up to 5 AI-generated suggested questions based on documents
    /// accessible to the authenticated employee in their effective departments.
    /// Falls back to 3 static questions if no documents exist or the AI is unavailable.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IReadOnlyList<SuggestedQuestionDto>>))]
    public async Task<IActionResult> GetSuggestions(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSuggestedQuestionsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SuggestedQuestionDto>>.Ok(result));
    }
}
