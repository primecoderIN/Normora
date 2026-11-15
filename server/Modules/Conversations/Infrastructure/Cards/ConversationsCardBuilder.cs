using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for past conversations and suggested starter questions.
/// </summary>
public sealed class ConversationsCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        toolName is "conversations_list" or "suggestions_get_popular" or "conversations_share" or "conversations_delete";

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(result.Output);
            var root = doc.RootElement;

            if (toolName == "conversations_share")
            {
                var shareUrl = root.TryGetProperty("shareUrl", out var sProp) ? sProp.GetString() : "";
                var token = root.TryGetProperty("shareToken", out var tProp) ? tProp.GetString() : "";

                return new ActionCardItem(
                    ActionType: "conversation_shared_card",
                    Title: "Conversation Link Ready",
                    Description: "Anyone in your organization with this link can view this conversation thread.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Share Link"] = shareUrl ?? "",
                        ["Token"] = token ?? "",
                        ["Access Scope"] = "Workspace Authorized",
                        ["route"] = shareUrl ?? "/employee/conversations"
                    },
                    ConfirmButtonText: "Open Shared View",
                    CancelButtonText: "Dismiss");
            }

            if (toolName == "conversations_delete")
            {
                var convId = root.TryGetProperty("conversationId", out var cProp) ? cProp.GetString() : "";

                return new ActionCardItem(
                    ActionType: "conversation_deleted_card",
                    Title: "Conversation Deleted",
                    Description: "The conversation thread and message turns have been removed.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Conversation ID"] = convId ?? "",
                        ["Status"] = "Deleted",
                        ["route"] = "/employee/conversations"
                    },
                    ConfirmButtonText: "New Conversation",
                    CancelButtonText: "Dismiss");
            }

            if (toolName == "suggestions_get_popular")
            {
                var parameters = new Dictionary<string, string>();
                if (root.TryGetProperty("questions", out var qArray) && qArray.ValueKind == JsonValueKind.Array)
                {
                    var index = 1;
                    foreach (var q in qArray.EnumerateArray().Take(4))
                    {
                        parameters[$"Suggestion {index++}"] = q.GetString() ?? "";
                    }
                }

                return new ActionCardItem(
                    ActionType: "suggestions_card",
                    Title: "Trending Policy Inquiries",
                    Description: "Frequently asked workplace topics and policy guidance.",
                    Parameters: parameters,
                    ConfirmButtonText: "Ask Another Question",
                    CancelButtonText: "Dismiss");
            }

            var total = root.TryGetProperty("totalConversations", out var t) ? t.GetInt32() : 0;
            var convParameters = new Dictionary<string, string>
            {
                ["Recent Threads"] = total.ToString(),
                ["route"] = "/employee/conversations"
            };

            if (root.TryGetProperty("conversations", out var convArray) && convArray.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var c in convArray.EnumerateArray().Take(3))
                {
                    var title = c.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "Conversation";
                    convParameters[$"Thread {index++}"] = title ?? "Conversation";
                }
            }

            return new ActionCardItem(
                ActionType: "conversations_list_card",
                Title: "Conversation History",
                Description: $"You have {total} previous conversational thread(s).",
                Parameters: convParameters,
                ConfirmButtonText: "View All Conversations",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}
