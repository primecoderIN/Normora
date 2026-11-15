using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for saved answers bookmarks and list results.
/// </summary>
public sealed class SavedAnswersCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        toolName is "saved_answers_list" or "saved_answers_save" or "saved_answers_unsave";

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

            if (toolName == "saved_answers_save")
            {
                var msgId = root.TryGetProperty("messageId", out var m) ? m.GetString() : "Answer";

                return new ActionCardItem(
                    ActionType: "answer_saved_card",
                    Title: "Answer Bookmarked to Notebook",
                    Description: "This response has been permanently saved to your employee notebook.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Message ID"] = msgId ?? "",
                        ["Notebook Status"] = "Bookmarked",
                        ["route"] = "/employee/saved-answers"
                    },
                    ConfirmButtonText: "Open Notebook",
                    CancelButtonText: "Dismiss");
            }

            if (toolName == "saved_answers_unsave")
            {
                var msgId = root.TryGetProperty("messageId", out var m) ? m.GetString() : "Answer";

                return new ActionCardItem(
                    ActionType: "answer_unsaved_card",
                    Title: "Bookmark Removed",
                    Description: "Answer removed from your personal notebook.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Message ID"] = msgId ?? "",
                        ["Notebook Status"] = "Removed",
                        ["route"] = "/employee/saved-answers"
                    },
                    ConfirmButtonText: "Open Notebook",
                    CancelButtonText: "Dismiss");
            }

            var total = root.TryGetProperty("totalSavedAnswers", out var t) ? t.GetInt32() : 0;
            var parameters = new Dictionary<string, string>
            {
                ["Bookmarked Answers"] = total.ToString(),
                ["route"] = "/employee/saved-answers"
            };

            if (root.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var item in itemsArray.EnumerateArray().Take(3))
                {
                    var excerpt = item.TryGetProperty("contentExcerpt", out var c) ? c.GetString() : "Excerpt";
                    parameters[$"Item {index++}"] = excerpt ?? "";
                }
            }

            return new ActionCardItem(
                ActionType: "saved_answers_list_card",
                Title: "Saved Answers Notebook",
                Description: $"You have {total} bookmarked answer(s) in your personal knowledge vault.",
                Parameters: parameters,
                ConfirmButtonText: "Open Saved Answers",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

