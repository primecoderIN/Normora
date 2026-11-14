using System.Text.Json;
using MediatR;
using Normora.Modules.Conversations.Application.SavedAnswers.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that queries the employee's bookmarked saved answers notebook.
/// </summary>
public sealed class SavedAnswersListTool(IMediator mediator) : IAgentTool
{
    public string Name => "saved_answers_list";

    public string Description =>
        "Lists the authenticated employee's bookmarked saved answers notebook, including content excerpts, saved dates, and grounded document citations.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Accessing saved answers notebook...",
        CompletedLabel: "Saved answers notebook retrieved",
        FailedLabel: "Failed to access saved answers");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            limit = new
            {
                type = "integer",
                description = "Maximum number of saved answers to retrieve (defaults to 10, max 50)."
            }
        },
        required = Array.Empty<string>()
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            var limit = root.TryGetProperty("limit", out var l) && l.TryGetInt32(out var parsedLimit)
                ? Math.Clamp(parsedLimit, 1, 50)
                : 10;

            var savedAnswers = await mediator.Send(new GetSavedAnswersQuery(limit, 0), cancellationToken);

            var result = new
            {
                totalSavedAnswers = savedAnswers.Count,
                items = savedAnswers.Select(a => new
                {
                    id = a.Id,
                    messageId = a.MessageId,
                    conversationId = a.ConversationId,
                    contentExcerpt = a.Content.Length > 120 ? a.Content[..120] + "..." : a.Content,
                    citationsCount = a.Citations.Count,
                    citations = a.Citations.Select(c => c.FileName).ToList(),
                    savedAt = a.SavedAt
                }).ToList()
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

