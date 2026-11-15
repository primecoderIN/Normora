using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Users.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that offboards an employee from the workspace (server-enforced HITL approval required).
/// </summary>
public sealed class WorkspaceRemoveEmployeeTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_remove_employee";

    public string Description =>
        "Revokes workspace membership and offboards an employee. Requires explicit human approval.";

    public ToolCategory Category => ToolCategory.Destructive;

    public bool RequiresApproval => true;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Removing employee...",
        CompletedLabel: "Employee access revoked",
        FailedLabel: "Failed to remove employee");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            membershipId = new { type = "string", format = "uuid", description = "The unique membership UUID of the employee to remove." }
        },
        required = new[] { "membershipId" }
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

            if (!root.TryGetProperty("membershipId", out var idProp) ||
                !Guid.TryParse(idProp.GetString(), out var memId))
            {
                return new AgentToolResult(false, string.Empty, "A valid membershipId UUID is required.");
            }

            var success = await mediator.Send(new RemoveEmployeeCommand(memId), cancellationToken);
            if (!success)
            {
                return new AgentToolResult(false, string.Empty, "Failed to remove the employee. Verify membership ID and last-admin invariant.");
            }

            var result = new
            {
                action = "employee_removed",
                membershipId = memId,
                status = "Access Revoked"
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
