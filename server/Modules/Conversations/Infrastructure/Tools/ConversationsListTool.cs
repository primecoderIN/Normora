using System.Text.Json;
using MediatR;
using Normora.Modules.Conversations.Application.Conversations.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that lists the authenticated employee's recent chat conversations.
/// </summary>
public sealed class ConversationsListTool(IMediator mediator) : IAgentTool
{
    public string Name => "conversations_list";

    public string Description =>
        "Lists the authenticated employee's recent chat conversations with their titles, summaries, and timestamps.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Accessing past conversations...",
        CompletedLabel: "Conversation history retrieved",
        FailedLabel: "Failed to access conversation history");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            limit = new
            {
                type = "integer",
                description = "Maximum number of conversations to retrieve (defaults to 10, max 30)."
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
                ? Math.Clamp(parsedLimit, 1, 30)
                : 10;

            var conversations = await mediator.Send(new GetConversationsQuery(limit, 0), cancellationToken);

            var result = new
            {
                totalConversations = conversations.Count,
                conversations = conversations.Select(c => new
                {
                    id = c.Id,
                    title = c.Title,
                    summary = c.Summary,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt,
                    lastMessageAt = c.LastMessageAt
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
