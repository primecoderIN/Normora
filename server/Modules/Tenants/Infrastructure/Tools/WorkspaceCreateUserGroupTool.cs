using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that provisions a new security user group within the workspace organization.
/// </summary>
public sealed class WorkspaceCreateUserGroupTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_create_user_group";

    public string Description =>
        "Provisions a new user security group within the workspace organization, with an optional description and initial department associations.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Creating user security group...",
        CompletedLabel: "User group successfully created",
        FailedLabel: "Failed to create user group");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            name = new
            {
                type = "string",
                description = "The display name of the user group (e.g. 'Engineering Leads', 'Contractors')."
            },
            description = new
            {
                type = "string",
                description = "Optional functional description explaining the group's purpose or permissions."
            },
            departmentIds = new
            {
                type = "array",
                items = new { type = "string" },
                description = "Optional list of department GUIDs to initially associate with this security group."
            }
        },
        required = new[] { "name" }
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                return new AgentToolResult(false, string.Empty, ApiMessages.ValidationFailed);
            }

            var description = root.TryGetProperty("description", out var d) ? d.GetString()?.Trim() : null;
            var departmentIds = new List<Guid>();
            if (root.TryGetProperty("departmentIds", out var depsProp) && depsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in depsProp.EnumerateArray())
                {
                    if (Guid.TryParse(element.GetString(), out var deptGuid))
                    {
                        departmentIds.Add(deptGuid);
                    }
                }
            }

            var groupId = await mediator.Send(new CreateUserGroupCommand(name, description, departmentIds), cancellationToken);

            var result = new
            {
                status = "created",
                groupId,
                name,
                description,
                associatedDepartmentsCount = departmentIds.Count,
                message = $"User group '{name}' has been successfully created in {context.TenantName}."
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result, ChangedData: true);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}

