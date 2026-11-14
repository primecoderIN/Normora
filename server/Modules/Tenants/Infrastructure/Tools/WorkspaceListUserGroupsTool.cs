using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that lists all user security groups configured within the workspace organization.
/// </summary>
public sealed class WorkspaceListUserGroupsTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_list_user_groups";

    public string Description =>
        "Lists all user security groups within the workspace, including member counts and associated departmental scopes.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Listing workspace user groups...",
        CompletedLabel: "Workspace user groups retrieved",
        FailedLabel: "Failed to list user groups");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = await mediator.Send(new GetUserGroupsQuery(), cancellationToken);

            var result = new
            {
                totalGroups = groups.Count,
                groups = groups.Select(g => new
                {
                    id = g.Id,
                    name = g.Name,
                    description = g.Description,
                    memberCount = g.MemberCount,
                    departmentCount = g.DepartmentCount,
                    departmentIds = g.DepartmentIds,
                    createdAt = g.CreatedAt
                }).ToList()
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
