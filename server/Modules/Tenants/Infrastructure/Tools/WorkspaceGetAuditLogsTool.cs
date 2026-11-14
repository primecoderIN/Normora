using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Audit.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that queries administrative and security audit logs for workspace compliance reporting.
/// </summary>
public sealed class WorkspaceGetAuditLogsTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_get_audit_logs";

    public string Description =>
        "Queries administrative and security audit logs within the workspace for compliance reporting, with optional action type and actor search filters.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Searching workspace audit trail...",
        CompletedLabel: "Audit trail events retrieved",
        FailedLabel: "Failed to query audit logs");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            action = new
            {
                type = "string",
                description = "Optional action type filter (e.g. 'Invitation.Sent', 'Department.Created', 'Document.Uploaded')."
            },
            search = new
            {
                type = "string",
                description = "Optional case-insensitive substring search matching actor email or user display name."
            },
            page = new
            {
                type = "integer",
                description = "Page number for pagination (defaults to 1)."
            },
            pageSize = new
            {
                type = "integer",
                description = "Number of audit log records to retrieve (defaults to 10, max 50)."
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

            var action = root.TryGetProperty("action", out var a) ? a.GetString() : null;
            var search = root.TryGetProperty("search", out var s) ? s.GetString() : null;
            var page = root.TryGetProperty("page", out var p) && p.TryGetInt32(out var parsedPage) ? Math.Max(1, parsedPage) : 1;
            var pageSize = root.TryGetProperty("pageSize", out var ps) && ps.TryGetInt32(out var parsedSize) ? Math.Clamp(parsedSize, 1, 50) : 10;

            var auditResult = await mediator.Send(
                new GetAuditLogsQuery(page, pageSize, action, search),
                cancellationToken);

            var result = new
            {
                totalEvents = auditResult.TotalCount,
                page = auditResult.Page,
                pageSize = auditResult.PageSize,
                logs = auditResult.Items.Select(l => new
                {
                    id = l.Id,
                    action = l.Action,
                    targetType = l.TargetType,
                    targetId = l.TargetId,
                    actorEmail = l.ActorEmail,
                    actorName = l.ActorName,
                    occurredAt = l.OccurredAt
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
