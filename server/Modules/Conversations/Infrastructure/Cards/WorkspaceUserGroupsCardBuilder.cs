using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for user group listing and creation results.
/// </summary>
public sealed class WorkspaceUserGroupsCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        toolName is "workspace_list_user_groups" or "workspace_create_user_group";

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

            if (toolName == "workspace_create_user_group")
            {
                var name = root.TryGetProperty("name", out var n) ? n.GetString() : "New User Group";
                var depts = root.TryGetProperty("associatedDepartmentsCount", out var d) ? d.GetInt32() : 0;

                return new ActionCardItem(
                    ActionType: "user_group_created_card",
                    Title: $"Security Group Created: {name}",
                    Description: $"Group provisioned with {depts} associated department scope(s).",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Group Name"] = name ?? "Group",
                        ["Department Scopes"] = depts.ToString(),
                        ["Status"] = "Active"
                    },
                    ConfirmButtonText: "Manage Group",
                    CancelButtonText: "Dismiss");
            }

            var total = root.TryGetProperty("totalGroups", out var t) ? t.GetInt32() : 0;
            var parameters = new Dictionary<string, string>
            {
                ["Total Groups"] = total.ToString()
            };

            if (root.TryGetProperty("groups", out var groupsProp) && groupsProp.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var g in groupsProp.EnumerateArray().Take(3))
                {
                    var gn = g.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Group";
                    var mc = g.TryGetProperty("memberCount", out var memProp) ? memProp.GetInt32() : 0;
                    parameters[$"Group {index++}"] = $"{gn} ({mc} members)";
                }
            }

            return new ActionCardItem(
                ActionType: "user_groups_list_card",
                Title: "Workspace Security Groups",
                Description: $"Configured with {total} user security group(s).",
                Parameters: parameters,
                ConfirmButtonText: "View Groups",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

