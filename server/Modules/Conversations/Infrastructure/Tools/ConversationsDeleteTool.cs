using System.Text.Json;
using MediatR;
using Normora.Modules.Conversations.Application.Conversations.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that deletes a conversation thread (server-enforced HITL approval required).
/// </summary>
public sealed class ConversationsDeleteTool(IMediator mediator) : IAgentTool
{
    public string Name => "conversations_delete";

    public string Description =>
        "Permanently deletes a conversation thread and its message history. Requires explicit human approval.";

    public ToolCategory Category => ToolCategory.Destructive;

    public bool RequiresApproval => true;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Deleting conversation thread...",
        CompletedLabel: "Conversation deleted",
        FailedLabel: "Failed to delete conversation");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            conversationId = new { type = "string", format = "uuid", description = "The unique conversation UUID to delete." }
        },
        required = new[] { "conversationId" }
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Guid? targetId = null;
            if (!string.IsNullOrWhiteSpace(argumentsJson) && argumentsJson != "{}")
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                if (doc.RootElement.TryGetProperty("conversationId", out var idProp) &&
                    Guid.TryParse(idProp.GetString(), out var parsedId))
                {
                    targetId = parsedId;
                }
            }

            targetId ??= context.ConversationId;

            if (targetId == null || targetId == Guid.Empty)
            {
                return new AgentToolResult(false, string.Empty, "A valid conversationId UUID is required.");
            }

            await mediator.Send(new DeleteConversationCommand(targetId.Value), cancellationToken);

            var result = new
            {
                action = "conversation_deleted",
                conversationId = targetId.Value,
                status = "Deleted"
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
