using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive HR escalation dispatch card when <c>escalate_hr_inquiry</c> finishes.
/// </summary>
public sealed class EscalateInquiryCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "escalate_hr_inquiry";

    public bool CanHandle(string toolName) =>
        string.Equals(toolName, TargetToolName, StringComparison.OrdinalIgnoreCase);

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;
            var inquiry = root.TryGetProperty("inquiry", out var inq) ? inq.GetString() : "Employee Policy Clarification";
            var category = root.TryGetProperty("category", out var cat) ? cat.GetString() : "General";
            var urgency = root.TryGetProperty("urgency", out var urg) ? urg.GetString() : "Normal";

            var parameters = new Dictionary<string, string>
            {
                ["Inquiry Subject"] = inquiry ?? "Policy Inquiry",
                ["Department Category"] = category ?? "General",
                ["Urgency Level"] = urgency ?? "Normal",
                ["Dispatch Status"] = result.Success ? "Successfully Sent to HR" : "Failed to Send"
            };

            return new ActionCardItem(
                ActionType: "hr_escalation_card",
                Title: "HR Administration Escalation",
                Description: result.Success
                    ? "Notification dispatched to workspace HR administrators for review."
                    : "Escalation notification could not be delivered.",
                Parameters: parameters,
                ConfirmButtonText: "View Inquiry Status",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

