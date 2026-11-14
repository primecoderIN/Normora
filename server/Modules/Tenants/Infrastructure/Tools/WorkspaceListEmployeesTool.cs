using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Users.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that queries the workspace employee directory with optional search, department, and group filters.
/// </summary>
public sealed class WorkspaceListEmployeesTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_list_employees";

    public string Description =>
        "Searches and lists active employees within the workspace directory with optional filters for name/email search, department, or user group.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Searching workspace employee directory...",
        CompletedLabel: "Employee directory retrieved",
        FailedLabel: "Failed to query employee directory");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            search = new
            {
                type = "string",
                description = "Optional search query to filter employees by email or display name."
            },
            page = new
            {
                type = "integer",
                description = "Page number for pagination (defaults to 1)."
            },
            pageSize = new
            {
                type = "integer",
                description = "Number of employees to return per page (defaults to 10, max 50)."
            },
            departmentId = new
            {
                type = "string",
                description = "Optional GUID to filter employees assigned to a specific department."
            },
            userGroupId = new
            {
                type = "string",
                description = "Optional GUID to filter employees assigned to a specific security user group."
            }
        },
        required = Array.Empty<string>()
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

            var search = root.TryGetProperty("search", out var s) ? s.GetString() : null;
            var page = root.TryGetProperty("page", out var p) && p.TryGetInt32(out var parsedPage) ? Math.Max(1, parsedPage) : 1;
            var pageSize = root.TryGetProperty("pageSize", out var ps) && ps.TryGetInt32(out var parsedSize) ? Math.Clamp(parsedSize, 1, 50) : 10;

            Guid? departmentId = null;
            if (root.TryGetProperty("departmentId", out var deptProp) && Guid.TryParse(deptProp.GetString(), out var parsedDept))
            {
                departmentId = parsedDept;
            }

            Guid? userGroupId = null;
            if (root.TryGetProperty("userGroupId", out var groupProp) && Guid.TryParse(groupProp.GetString(), out var parsedGroup))
            {
                userGroupId = parsedGroup;
            }

            var pagedEmployees = await mediator.Send(
                new GetTenantEmployeesQuery(context.TenantId, page, pageSize, search, departmentId, userGroupId),
                cancellationToken);

            var result = new
            {
                totalCount = pagedEmployees.TotalCount,
                page = pagedEmployees.Page,
                pageSize = pagedEmployees.PageSize,
                employees = pagedEmployees.Items.Select(e => new
                {
                    membershipId = e.MembershipId,
                    userId = e.UserId,
                    email = e.Email,
                    displayName = e.DisplayName,
                    role = e.Role,
                    departments = e.Departments.Select(d => d.Name).ToList(),
                    userGroups = e.UserGroups.Select(g => g.Name).ToList(),
                    joinedAt = e.JoinedAt
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
