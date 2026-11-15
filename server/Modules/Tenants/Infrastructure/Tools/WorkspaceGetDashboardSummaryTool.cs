using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Dashboard.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that queries the executive workspace dashboard summary including document counts, storage, and department breakdown.
/// </summary>
public sealed class WorkspaceGetDashboardSummaryTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_get_dashboard_summary";

    public string Description =>
        "Retrieves an executive summary of the workspace: total active documents, processing status, storage consumed, member headcount, and department breakdown.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Gathering workspace analytics summary...",
        CompletedLabel: "Workspace summary metrics assembled",
        FailedLabel: "Failed to assemble workspace summary");

    public object? ParameterSchema => null;

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);

            var result = new
            {
                workspace = context.TenantName,
                totalDocuments = summary.Stats.TotalDocuments,
                documentsTrend = summary.Stats.DocumentsTrend,
                totalEmployees = summary.Stats.TotalEmployees,
                employeesTrend = summary.Stats.EmployeesTrend,
                totalQuestions = summary.Stats.TotalQuestions,
                questionsTrend = summary.Stats.QuestionsTrend,
                answerQuality = summary.Stats.AnswerQuality,
                departmentCoverageCount = summary.DocumentCoverage.Count,
                recentDocumentsCount = summary.RecentDocuments.Count,
                topQuestionsCount = summary.TopQuestions.Count
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: summary);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
