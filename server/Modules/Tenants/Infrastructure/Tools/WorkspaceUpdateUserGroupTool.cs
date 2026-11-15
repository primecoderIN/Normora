using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that updates a user group's name, description, and department scopes.
/// </summary>
public sealed class WorkspaceUpdateUserGroupTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_update_user_group";

    public string Description =>
        "Updates the name, description, and department clearance mappings of an existing user security group.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Updating user group...",
        CompletedLabel: "User group updated",
        FailedLabel: "Failed to update user group");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            groupId = new { type = "string", format = "uuid", description = "The unique identifier of the user group to update." },
            name = new { type = "string", description = "The updated name for the user group." },
            description = new { type = "string", description = "The updated description for the user group." },
            departmentIds = new
            {
                type = "array",
                items = new { type = "string", format = "uuid" },
                description = "Optional list of department scope UUIDs associated with this user group."
            }
        },
        required = new[] { "groupId", "name" }
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

            if (!root.TryGetProperty("name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
            {
                return new AgentToolResult(false, string.Empty, "User group name is required.");
            }

            var name = nameProp.GetString()!;
            var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            var departmentIds = new List<Guid>();
            if (root.TryGetProperty("departmentIds", out var deptArray) && deptArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in deptArray.EnumerateArray())
                {
                    if (Guid.TryParse(el.GetString(), out var dId))
                        departmentIds.Add(dId);
                }
            }

            var success = await mediator.Send(new UpdateUserGroupCommand(grpId, name, description, departmentIds), cancellationToken);
            if (!success)
            {
                return new AgentToolResult(false, string.Empty, "Failed to update the specified user group.");
            }

            var result = new
            {
                action = "user_group_updated",
                groupId = grpId,
                name,
                description,
                departmentCount = departmentIds.Count,
                status = "Updated"
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
