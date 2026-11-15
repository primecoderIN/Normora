using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for workspace white-label branding and styling configurations.
/// </summary>
public sealed class WorkspaceBrandingCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) => toolName == "workspace_get_branding";

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(result.Output);
            var root = doc.RootElement;

            var wsName = root.TryGetProperty("workspaceName", out var nProp) ? nProp.GetString() : "Workspace";
            var slug = root.TryGetProperty("slug", out var sProp) ? sProp.GetString() : "slug";
            var primary = root.TryGetProperty("primaryColor", out var pProp) ? pProp.GetString() : "#ec4899";
            var secondary = root.TryGetProperty("secondaryColor", out var scProp) ? scProp.GetString() : "#6366f1";

            return new ActionCardItem(
                ActionType: "workspace_branding_card",
                Title: $"{wsName} Branding",
                Description: "Workspace vanity styling, primary/secondary colors, and white-label asset links.",
                Parameters: new Dictionary<string, string>
                {
                    ["Workspace Name"] = wsName ?? "",
                    ["Vanity Slug"] = slug ?? "",
                    ["Primary Hex"] = primary ?? "",
                    ["Secondary Hex"] = secondary ?? "",
                    ["route"] = "/employer/settings/branding"
                },
                ConfirmButtonText: "Customize Branding",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}
