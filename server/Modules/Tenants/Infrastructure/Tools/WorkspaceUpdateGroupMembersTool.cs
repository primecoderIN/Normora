using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that updates employee membership assignments and department scopes for a user group.
/// </summary>
public sealed class WorkspaceUpdateGroupMembersTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_update_group_members";

    public string Description =>
        "Synchronizes the assigned employee members and department clearance scopes for a specified user group.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Updating group membership...",
        CompletedLabel: "Group memberships synchronized",
        FailedLabel: "Failed to update group memberships");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            groupId = new { type = "string", format = "uuid", description = "The unique identifier of the user group." },
            memberUserIds = new
            {
                type = "array",
                items = new { type = "string", format = "uuid" },
                description = "Array of user UUIDs to assign to this group."
            },
            departmentIds = new
            {
                type = "array",
                items = new { type = "string", format = "uuid" },
                description = "Array of department UUIDs to grant to this group."
            }
        },
        required = new[] { "groupId" }
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("groupId", out var idProp) ||
                !Guid.TryParse(idProp.GetString(), out var grpId))
            {
                return new AgentToolResult(false, string.Empty, "A valid groupId UUID is required.");
            }

            var memberUserIds = new List<Guid>();
            if (root.TryGetProperty("memberUserIds", out var membersArray) && membersArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in membersArray.EnumerateArray())
                {
                    if (Guid.TryParse(el.GetString(), out var uId))
                        memberUserIds.Add(uId);
                }
            }

            var departmentIds = new List<Guid>();
            if (root.TryGetProperty("departmentIds", out var deptsArray) && deptsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in deptsArray.EnumerateArray())
                {
                    if (Guid.TryParse(el.GetString(), out var dId))
                        departmentIds.Add(dId);
                }
            }

            var success = await mediator.Send(new UpdateUserGroupAssignmentsCommand(grpId, memberUserIds, departmentIds), cancellationToken);
            if (!success)
            {
                return new AgentToolResult(false, string.Empty, "Failed to update user group membership assignments.");
            }

            var result = new
            {
                action = "group_members_updated",
                groupId = grpId,
                totalMembers = memberUserIds.Count,
                totalDepartments = departmentIds.Count,
                status = "Synchronized"
            };

            var outputJson = JsonSerializer.Serialize(result);
            return new AgentToolResult(true, outputJson, ChangedData: true, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
