using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Action card builder for the dynamic <c>workspace_request_form</c> intake tool.
/// Transforms field specifications into an interactive A2UI intake form card.
/// </summary>
public sealed class IntakeFormCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        string.Equals(toolName, "workspace_request_form", StringComparison.OrdinalIgnoreCase);

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        var title = "Workspace Action Form";
        var description = "Please fill out the requested fields below.";
        var actionTarget = "";
        var fieldsJson = "[]";

        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(result.Output) ? argumentsJson : result.Output);
            var root = doc.RootElement;

            if (root.TryGetProperty("title", out var titleProp))
            {
                title = titleProp.GetString() ?? title;
            }

            if (root.TryGetProperty("description", out var descProp))
            {
                description = descProp.GetString() ?? description;
            }

            if (root.TryGetProperty("actionTarget", out var targetProp))
            {
                actionTarget = targetProp.GetString() ?? "";
            }

            if (root.TryGetProperty("fieldsJson", out var fieldsProp))
            {
                fieldsJson = fieldsProp.GetString() ?? "[]";
            }
        }
        catch
        {
            // fallback defaults
        }

        var parameters = new Dictionary<string, string>
        {
            ["actionTarget"] = actionTarget,
            ["fields"] = fieldsJson
        };

        return new ActionCardItem(
            ActionType: "intake_form_card",
            Title: title,
            Description: description,
            Parameters: parameters,
            ConfirmButtonText: "Submit Information",
            CancelButtonText: "Cancel");
    }
}

