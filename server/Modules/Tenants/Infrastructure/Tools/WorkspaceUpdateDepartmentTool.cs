using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Departments.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that updates a department's name or description in the workspace.
/// </summary>
public sealed class WorkspaceUpdateDepartmentTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_update_department";

    public string Description =>
        "Updates the name or description of an existing organizational department in the workspace.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Updating department...",
        CompletedLabel: "Department updated",
        FailedLabel: "Failed to update department");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            departmentId = new { type = "string", format = "uuid", description = "The unique identifier of the department to update." },
            name = new { type = "string", description = "The updated name for the department." },
            description = new { type = "string", description = "The updated description for the department." }
        },
        required = new[] { "departmentId", "name" }
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

            if (!root.TryGetProperty("departmentId", out var idProp) ||
                !Guid.TryParse(idProp.GetString(), out var deptId))
            {
                return new AgentToolResult(false, string.Empty, "A valid departmentId UUID is required.");
            }

            if (!root.TryGetProperty("name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
            {
                return new AgentToolResult(false, string.Empty, "Department name is required.");
            }

            var name = nameProp.GetString()!;
            var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;

            var success = await mediator.Send(new UpdateDepartmentCommand(deptId, name, description), cancellationToken);
            if (!success)
            {
                return new AgentToolResult(false, string.Empty, "Failed to update the specified department.");
            }

            var result = new
            {
                action = "department_updated",
                departmentId = deptId,
                name,
                description,
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
