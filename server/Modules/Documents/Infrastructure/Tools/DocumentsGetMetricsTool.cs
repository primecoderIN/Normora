using System.Text.Json;
using MediatR;
using Normora.Shared.Constants;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that queries knowledge base document processing pipeline metrics.
/// </summary>
public sealed class DocumentsGetMetricsTool(IMediator mediator) : IAgentTool
{
    public string Name => "documents_get_metrics";

    public string Description =>
        "Retrieves knowledge base processing pipeline metrics (total indexed, currently processing, failed documents, and recent uploads).";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Gathering document processing metrics...",
        CompletedLabel: "Document metrics assembled",
        FailedLabel: "Failed to assemble document metrics");

    public object? ParameterSchema => null;

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await mediator.Send(new GetDocumentDashboardMetricsQuery(context.TenantId), cancellationToken);

            var result = new
            {
                totalDocuments = metrics.TotalDocuments,
                documentsTrend = metrics.DocumentsTrend,
                failedDocsCount = metrics.FailedDocsCount,
                processingDocsCount = metrics.ProcessingDocsCount,
                totalFileNamesCount = metrics.FileNames.Count,
                recentUploads = metrics.RecentDocuments.Select(r => new
                {
                    fileName = r.FileName,
                    status = r.Status,
                    type = r.Type,
                    uploadedAt = r.UploadedAt
                }).ToList()
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: metrics);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
