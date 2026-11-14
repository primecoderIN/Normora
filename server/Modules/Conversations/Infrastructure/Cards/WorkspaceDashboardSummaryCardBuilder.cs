using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an executive overview card when <c>workspace_get_dashboard_summary</c> finishes.
/// </summary>
public sealed class WorkspaceDashboardSummaryCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "workspace_get_dashboard_summary";

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

            var workspace = root.TryGetProperty("workspace", out var w) ? w.GetString() : "Organization";
            var docs = root.TryGetProperty("totalDocuments", out var td) ? td.GetInt32() : 0;
            var employees = root.TryGetProperty("totalEmployees", out var te) ? te.GetInt32() : 0;
            var questions = root.TryGetProperty("totalQuestions", out var tq) ? tq.GetInt32() : 0;
            var quality = root.TryGetProperty("answerQuality", out var aq) ? aq.GetDouble() : 0.0;

            return new ActionCardItem(
                ActionType: "dashboard_summary_card",
                Title: $"{workspace} Overview",
                Description: "High-level metrics across workspace knowledge, staff, and AI inquiries.",
                Parameters: new Dictionary<string, string>
                {
                    ["Active Documents"] = docs.ToString(),
                    ["Enrolled Employees"] = employees.ToString(),
                    ["Inquiries Processed"] = questions.ToString(),
                    ["Answer Rating"] = $"{quality:P0}"
                },
                ConfirmButtonText: "Open Dashboard",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

