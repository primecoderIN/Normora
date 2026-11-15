using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for department listing and creation results.
/// </summary>
public sealed class WorkspaceDepartmentsCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        toolName is "workspace_list_departments" or "workspace_create_department" or "workspace_update_department" or "workspace_delete_department";

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

            if (toolName == "workspace_create_department")
            {
                var name = root.TryGetProperty("name", out var n) ? n.GetString() : "New Department";
                var description = root.TryGetProperty("description", out var d) ? d.GetString() : "Standard organizational division";

                return new ActionCardItem(
                    ActionType: "department_created_card",
                    Title: $"Department Created: {name}",
                    Description: description ?? "New workspace department provisioned.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Department Name"] = name ?? "Department",
                        ["Status"] = "Active",
                        ["route"] = "/employer/settings/departments"
                    },
                    ConfirmButtonText: "View Departments",
                    CancelButtonText: "Dismiss");
            }

            if (toolName == "workspace_update_department")
            {
                var name = root.TryGetProperty("name", out var n) ? n.GetString() : "Department";
                var description = root.TryGetProperty("description", out var d) ? d.GetString() : null;

                return new ActionCardItem(
                    ActionType: "department_updated_card",
                    Title: $"Department Updated: {name}",
                    Description: description ?? "Department details updated.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Department Name"] = name ?? "Department",
                        ["Status"] = "Updated",
                        ["route"] = "/employer/settings/departments"
                    },
                    ConfirmButtonText: "View Departments",
                    CancelButtonText: "Dismiss");
            }

            if (toolName == "workspace_delete_department")
            {
                var deptId = root.TryGetProperty("departmentId", out var idProp) ? idProp.GetString() : "";

                return new ActionCardItem(
                    ActionType: "department_deleted_card",
                    Title: "Department Deleted",
                    Description: "The department was removed from the organization.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Department ID"] = deptId ?? "",
                        ["Status"] = "Deleted",
                        ["route"] = "/employer/settings/departments"
                    },
                    ConfirmButtonText: "View Departments",
                    CancelButtonText: "Dismiss");
            }

            // List departments
            var total = root.TryGetProperty("totalDepartments", out var t) ? t.GetInt32() : 0;
            var parameters = new Dictionary<string, string>
            {
                ["Total Departments"] = total.ToString(),
                ["route"] = "/employer/settings/departments"
            };

            if (root.TryGetProperty("departments", out var deptsProp) && deptsProp.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var dept in deptsProp.EnumerateArray().Take(4))
                {
                    if (dept.TryGetProperty("name", out var dn))
                    {
                        parameters[$"Dept {index++}"] = dn.GetString() ?? "";
                    }
                }
            }

            return new ActionCardItem(
                ActionType: "departments_list_card",
                Title: "Workspace Departments",
                Description: $"Organization configured with {total} departmental unit(s).",
                Parameters: parameters,
                ConfirmButtonText: "Manage Departments",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

