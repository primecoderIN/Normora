using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that deletes a user security group from the workspace (server-enforced HITL approval required).
/// </summary>
public sealed class WorkspaceDeleteUserGroupTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_delete_user_group";

    public string Description =>
        "Permanently deletes a user security group from the workspace. Requires explicit human approval.";

    public ToolCategory Category => ToolCategory.Destructive;

    public bool RequiresApproval => true;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Deleting user group...",
        CompletedLabel: "User group deleted",
        FailedLabel: "Failed to delete user group");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            groupId = new { type = "string", format = "uuid", description = "The unique identifier of the user group to delete." }
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

            var success = await mediator.Send(new DeleteUserGroupCommand(grpId), cancellationToken);
            if (!success)
            {
                return new AgentToolResult(false, string.Empty, "Failed to delete the specified user group.");
            }

            var result = new
            {
                action = "user_group_deleted",
                groupId = grpId,
                status = "Deleted"
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
