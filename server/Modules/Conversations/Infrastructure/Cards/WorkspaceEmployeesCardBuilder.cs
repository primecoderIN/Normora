using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for workspace employee directory queries.
/// </summary>
public sealed class WorkspaceEmployeesCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "workspace_list_employees";

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

            var totalCount = root.TryGetProperty("totalCount", out var tc) ? tc.GetInt32() : 0;
            var parameters = new Dictionary<string, string>
            {
                ["Total Matches"] = totalCount.ToString()
            };

            if (root.TryGetProperty("employees", out var empArray) && empArray.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var emp in empArray.EnumerateArray().Take(3))
                {
                    var name = emp.TryGetProperty("displayName", out var dn) ? dn.GetString() : "Employee";
                    var role = emp.TryGetProperty("role", out var r) ? r.GetString() : "Member";
                    var email = emp.TryGetProperty("email", out var em) ? em.GetString() : "";
                    parameters[$"Member {index++}"] = $"{name} ({role}) - {email}";
                }
            }

            return new ActionCardItem(
                ActionType: "employees_list_card",
                Title: "Workspace Directory",
                Description: $"Discovered {totalCount} matching employee record(s).",
                Parameters: parameters,
                ConfirmButtonText: "Open Directory",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

