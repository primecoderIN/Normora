using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive compliance card when <c>workspace_get_audit_logs</c> finishes.
/// </summary>
public sealed class WorkspaceAuditLogsCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "workspace_get_audit_logs";

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

            var total = root.TryGetProperty("totalEvents", out var te) ? te.GetInt32() : 0;
            var parameters = new Dictionary<string, string>
            {
                ["Total Audit Records"] = total.ToString()
            };

            if (root.TryGetProperty("logs", out var logsArray) && logsArray.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var log in logsArray.EnumerateArray().Take(3))
                {
                    var action = log.TryGetProperty("action", out var a) ? a.GetString() : "Action";
                    var actor = log.TryGetProperty("actorEmail", out var ae) ? ae.GetString() : "System";
                    parameters[$"Event {index++}"] = $"{action} by {actor}";
                }
            }

            return new ActionCardItem(
                ActionType: "audit_logs_card",
                Title: "Security Audit Trail",
                Description: $"Retrieved {total} compliance event(s) from the audit log.",
                Parameters: parameters,
                ConfirmButtonText: "View Full Trail",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

