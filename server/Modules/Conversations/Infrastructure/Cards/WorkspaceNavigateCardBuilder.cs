using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive navigation card with 1-click UI actuation for workspace views.
/// </summary>
public sealed class WorkspaceNavigateCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) => toolName == "workspace_navigate";

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(result.Output);
            var root = doc.RootElement;

            var title = root.TryGetProperty("title", out var tProp) ? tProp.GetString() : "Workspace View";
            var description = root.TryGetProperty("description", out var dProp) ? dProp.GetString() : "Navigate to this workspace area.";
            var route = root.TryGetProperty("route", out var rProp) ? rProp.GetString() : "/employer/dashboard";
            var destination = root.TryGetProperty("destination", out var dstProp) ? dstProp.GetString() : "dashboard";

            return new ActionCardItem(
                ActionType: "workspace_navigate_card",
                Title: title ?? "Workspace View",
                Description: description ?? string.Empty,
                Parameters: new Dictionary<string, string>
                {
                    ["Destination View"] = title ?? destination ?? "",
                    ["Target Route"] = route ?? "",
                    ["route"] = route ?? ""
                },
                ConfirmButtonText: $"Open {title}",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}
