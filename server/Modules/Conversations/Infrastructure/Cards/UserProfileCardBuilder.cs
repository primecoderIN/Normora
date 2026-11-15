using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card displaying the authenticated user's profile and workspace memberships.
/// </summary>
public sealed class UserProfileCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) => toolName == "user_get_profile";

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(result.Output);
            var root = doc.RootElement;

            var displayName = root.TryGetProperty("displayName", out var nProp) ? nProp.GetString() : "User";
            var email = root.TryGetProperty("email", out var eProp) ? eProp.GetString() : "";
            var role = root.TryGetProperty("currentRole", out var rProp) ? rProp.GetString() : "Employee";
            var workspace = root.TryGetProperty("currentWorkspace", out var wProp) ? wProp.GetString() : "Workspace";

            return new ActionCardItem(
                ActionType: "user_profile_card",
                Title: $"{displayName}'s Profile",
                Description: "Authenticated identity, active workspace tenant, and permissions.",
                Parameters: new Dictionary<string, string>
                {
                    ["Display Name"] = displayName ?? "",
                    ["Email Address"] = email ?? "",
                    ["Current Role"] = role ?? "",
                    ["Active Workspace"] = workspace ?? "",
                    ["route"] = "/employer/profile"
                },
                ConfirmButtonText: "Open Profile Settings",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}
