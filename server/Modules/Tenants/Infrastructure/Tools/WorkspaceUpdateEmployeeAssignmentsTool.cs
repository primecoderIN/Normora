using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Users.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that updates an employee's department clearances and security user groups.
/// </summary>
public sealed class WorkspaceUpdateEmployeeAssignmentsTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_update_employee_assignments";

    public string Description =>
        "Configures an employee's direct department clearances and assigned user security groups.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Updating employee clearances...",
        CompletedLabel: "Employee clearances configured",
        FailedLabel: "Failed to update employee clearances");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            userId = new { type = "string", format = "uuid", description = "The unique user UUID whose clearances are being updated." },
            departmentIds = new
            {
                type = "array",
                items = new { type = "string", format = "uuid" },
                description = "Department clearance UUIDs directly assigned to this employee."
            },
            userGroupIds = new
            {
                type = "array",
                items = new { type = "string", format = "uuid" },
                description = "User security group UUIDs assigned to this employee."
            }
        },
        required = new[] { "userId" }
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

            if (!root.TryGetProperty("userId", out var idProp) ||
                !Guid.TryParse(idProp.GetString(), out var uId))
            {
                return new AgentToolResult(false, string.Empty, "A valid userId UUID is required.");
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

            var userGroupIds = new List<Guid>();
            if (root.TryGetProperty("userGroupIds", out var grpsArray) && grpsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in grpsArray.EnumerateArray())
                {
                    if (Guid.TryParse(el.GetString(), out var gId))
                        userGroupIds.Add(gId);
                }
            }

            var success = await mediator.Send(new UpdateUserAssignmentsCommand(uId, departmentIds, userGroupIds), cancellationToken);
            if (!success)
            {
                return new AgentToolResult(false, string.Empty, "Failed to update employee clearance assignments.");
            }

            var result = new
            {
                action = "employee_assignments_updated",
                userId = uId,
                assignedDepartments = departmentIds.Count,
                assignedUserGroups = userGroupIds.Count,
                status = "Configured"
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
