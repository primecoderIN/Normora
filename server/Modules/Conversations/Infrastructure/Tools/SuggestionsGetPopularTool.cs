using System.Text.Json;
using MediatR;
using Normora.Modules.Conversations.Application.Suggestions.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that retrieves suggested starter and popular policy inquiry questions.
/// </summary>
public sealed class SuggestionsGetPopularTool(IMediator mediator) : IAgentTool
{
    public string Name => "suggestions_get_popular";

    public string Description =>
        "Retrieves suggested starter questions and trending organizational inquiries derived from company documentation.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Gathering trending policy questions...",
        CompletedLabel: "Suggested questions assembled",
        FailedLabel: "Failed to assemble suggestions");

    public object? ParameterSchema => null;

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var suggestions = await mediator.Send(new GetSuggestedQuestionsQuery(), cancellationToken);

            var result = new
            {
                totalSuggestions = suggestions.Count,
                questions = suggestions.Select(s => s.Question).ToList()
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
