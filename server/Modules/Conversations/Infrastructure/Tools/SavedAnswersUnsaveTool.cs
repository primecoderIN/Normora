using System.Text.Json;
using MediatR;
using Normora.Modules.Conversations.Application.SavedAnswers.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that removes a bookmarked answer from the user's saved answers.
/// </summary>
public sealed class SavedAnswersUnsaveTool(IMediator mediator) : IAgentTool
{
    public string Name => "saved_answers_unsave";

    public string Description =>
        "Removes a previously bookmarked policy answer from the user's saved answers collection.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Removing bookmark...",
        CompletedLabel: "Answer removed from saved answers",
        FailedLabel: "Failed to remove saved answer");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            messageId = new { type = "string", format = "uuid", description = "The unique message UUID to un-bookmark." }
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
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("messageId", out var idProp) ||
                !Guid.TryParse(idProp.GetString(), out var msgId))
            {
                return new AgentToolResult(false, string.Empty, "A valid messageId UUID is required.");
            }

            await mediator.Send(new UnsaveAnswerCommand(msgId), cancellationToken);

            var result = new
            {
                action = "answer_unsaved",
                messageId = msgId,
                status = "Removed"
            };

            var outputJson = JsonSerializer.Serialize(result);
            return new AgentToolResult(true, outputJson, ChangedData: true, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
