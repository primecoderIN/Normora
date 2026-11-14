using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Departments.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that provisions a new organizational department within the active workspace.
/// </summary>
public sealed class WorkspaceCreateDepartmentTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_create_department";

    public string Description =>
        "Provisions a new organizational department within the workspace with a required unique name and optional functional description.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Creating organizational department...",
        CompletedLabel: "Department successfully created",
        FailedLabel: "Failed to create department");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            name = new
            {
                type = "string",
                description = "The display name of the new department (e.g., 'Finance', 'Engineering', 'Customer Support')."
            },
            description = new
            {
                type = "string",
                description = "Optional description explaining the organizational purpose or responsibilities of this department."
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

            var departmentId = await mediator.Send(new CreateDepartmentCommand(name, description), cancellationToken);

            var result = new
            {
                status = "created",
                departmentId,
                name,
                description,
                message = $"Department '{name}' has been successfully created in {context.TenantName}."
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

