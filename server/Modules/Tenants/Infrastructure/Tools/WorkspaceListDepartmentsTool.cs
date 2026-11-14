using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Departments.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that queries all active departments configured in the current tenant workspace.
/// </summary>
public sealed class WorkspaceListDepartmentsTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_list_departments";

    public string Description =>
        "Lists all active departments configured within the workspace organization, including department names and unique identifiers.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Listing workspace departments...",
        CompletedLabel: "Workspace departments retrieved",
        FailedLabel: "Failed to retrieve departments");

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
            var departments = await mediator.Send(new GetDepartmentsQuery(), cancellationToken);

            var summary = new
            {
                totalDepartments = departments.Count,
                departments = departments.Select(d => new
                {
                    id = d.Id,
                    name = d.Name
                }).ToList()
            };

            var outputJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: summary);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}

