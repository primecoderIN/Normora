using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for workspace employee directory queries.
/// </summary>
public sealed class WorkspaceEmployeesCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        toolName is "workspace_list_employees" or "workspace_remove_employee" or "workspace_update_employee_assignments";

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

            if (toolName == "workspace_remove_employee")
            {
                var memId = root.TryGetProperty("membershipId", out var idProp) ? idProp.GetString() : "";

                return new ActionCardItem(
                    ActionType: "employee_removed_card",
                    Title: "Employee Offboarded",
                    Description: "Workspace access and security group memberships have been revoked.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Membership ID"] = memId ?? "",
                        ["Status"] = "Access Revoked",
                        ["route"] = "/employer/employees"
                    },
                    ConfirmButtonText: "Open Directory",
                    CancelButtonText: "Dismiss");
            }

            if (toolName == "workspace_update_employee_assignments")
            {
                var depts = root.TryGetProperty("assignedDepartments", out var d) ? d.GetInt32() : 0;
                var groups = root.TryGetProperty("assignedUserGroups", out var g) ? g.GetInt32() : 0;

                return new ActionCardItem(
                    ActionType: "employee_assignments_card",
                    Title: "Employee Clearances Updated",
                    Description: "Direct department assignments and security groups synchronized.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Department Clearances"] = depts.ToString(),
                        ["Security Groups"] = groups.ToString(),
                        ["Status"] = "Configured",
                        ["route"] = "/employer/employees"
                    },
                    ConfirmButtonText: "Open Directory",
                    CancelButtonText: "Dismiss");
            }

            var totalCount = root.TryGetProperty("totalCount", out var tc) ? tc.GetInt32() : 0;
            var parameters = new Dictionary<string, string>
            {
                ["Total Matches"] = totalCount.ToString(),
                ["route"] = "/employer/employees"
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

