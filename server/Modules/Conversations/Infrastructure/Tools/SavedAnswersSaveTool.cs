using System.Text.Json;
using MediatR;
using Normora.Modules.Conversations.Application.SavedAnswers.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that bookmarks an AI response turn into the employee's permanent notebook.
/// </summary>
public sealed class SavedAnswersSaveTool(IMediator mediator) : IAgentTool
{
    public string Name => "saved_answers_save";

    public string Description =>
        "Bookmarks a specific assistant answer message into the employee's permanent saved answers notebook by message GUID.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Saving answer to notebook...",
        CompletedLabel: "Answer saved to notebook",
        FailedLabel: "Failed to bookmark answer");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            messageId = new
            {
                type = "string",
                description = "The unique GUID identifier of the assistant message turn to bookmark."
            }
        },
        required = new[] { "messageId" }
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

            if (!root.TryGetProperty("messageId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var messageId))
            {
                return new AgentToolResult(false, string.Empty, ApiMessages.ValidationFailed);
            }

            var savedAnswerId = await mediator.Send(new SaveAnswerCommand(messageId), cancellationToken);

            var result = new
            {
                status = "saved",
                savedAnswerId,
                messageId,
                message = "The answer has been successfully bookmarked to your permanent notebook."
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result, ChangedData: true);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}

