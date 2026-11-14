using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card when an employee workspace invitation is issued.
/// </summary>
public sealed class WorkspaceInvitationsCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "workspace_invite_employee";

    public bool CanHandle(string toolName) =>
        string.Equals(toolName, TargetToolName, StringComparison.OrdinalIgnoreCase);

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

            var email = root.TryGetProperty("email", out var e) ? e.GetString() : "employee@company.com";
            var org = root.TryGetProperty("organization", out var o) ? o.GetString() : "Workspace";
            var hours = root.TryGetProperty("expiresInHours", out var h) ? h.GetInt32() : 48;

            return new ActionCardItem(
                ActionType: "invitation_sent_card",
                Title: "Workspace Invitation Dispatched",
                Description: $"An invitation has been sent to {email} to join {org}.",
                Parameters: new Dictionary<string, string>
                {
                    ["Recipient Email"] = email ?? "",
                    ["Invitation Validity"] = $"{hours} hours",
                    ["Status"] = "Pending Acceptance"
                },
                ConfirmButtonText: "View Invitations",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

