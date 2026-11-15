using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Departments.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that deletes a department from the workspace (server-enforced HITL approval required).
/// </summary>
public sealed class WorkspaceDeleteDepartmentTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_delete_department";

    public string Description =>
        "Permanently deletes an organizational department from the workspace. Requires explicit human approval.";

    public ToolCategory Category => ToolCategory.Destructive;

    public bool RequiresApproval => true;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Deleting department...",
        CompletedLabel: "Department deleted",
        FailedLabel: "Failed to delete department");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            departmentId = new { type = "string", format = "uuid", description = "The unique identifier of the department to delete." }
        },
        required = new[] { "departmentId" }
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

            var success = await mediator.Send(new DeleteDepartmentCommand(deptId), cancellationToken);
            if (!success)
            {
                return new AgentToolResult(false, string.Empty, "Failed to delete the specified department.");
            }

            var result = new
            {
                action = "department_deleted",
                departmentId = deptId,
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
