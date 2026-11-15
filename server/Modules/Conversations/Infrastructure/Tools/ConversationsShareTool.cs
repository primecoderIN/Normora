using System.Text.Json;
using MediatR;
using Normora.Modules.Conversations.Application.Conversations.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that generates or retrieves a secure shareable link for a conversation thread.
/// </summary>
public sealed class ConversationsShareTool(IMediator mediator) : IAgentTool
{
    public string Name => "conversations_share";

    public string Description =>
        "Generates a secure high-entropy share link for a conversation thread to share with team members.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Generating share link...",
        CompletedLabel: "Conversation share link generated",
        FailedLabel: "Failed to generate share link");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            conversationId = new { type = "string", format = "uuid", description = "Optional conversation UUID. Defaults to the active conversation." }
        }
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
                return new AgentToolResult(false, string.Empty, "No conversation identifier available to share.");
            }

            var share = await mediator.Send(new ShareConversationCommand(targetId.Value), cancellationToken);

            var result = new
            {
                action = "conversation_shared",
                conversationId = targetId.Value,
                shareToken = share.Token,
                shareUrl = $"/employee/conversations/shared/{share.Token}",
                expiresAt = share.ExpiresAt,
                isShared = true
            };

            var outputJson = JsonSerializer.Serialize(result);
            return new AgentToolResult(true, outputJson, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
