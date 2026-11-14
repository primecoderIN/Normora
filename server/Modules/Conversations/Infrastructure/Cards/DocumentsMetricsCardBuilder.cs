using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for document processing pipeline metrics.
/// </summary>
public sealed class DocumentsMetricsCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "documents_get_metrics";

    public bool CanHandle(string toolName) =>
        string.Equals(toolName, TargetToolName, StringComparison.OrdinalIgnoreCase);

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(result.Output);
            var root = doc.RootElement;

            var total = root.TryGetProperty("totalDocuments", out var t) ? t.GetInt32() : 0;
            var processing = root.TryGetProperty("processingDocsCount", out var p) ? p.GetInt32() : 0;
            var failed = root.TryGetProperty("failedDocsCount", out var f) ? f.GetInt32() : 0;

            return new ActionCardItem(
                ActionType: "document_metrics_card",
                Title: "Document Ingestion Pipeline",
                Description: "Real-time processing status across organization document versions.",
                Parameters: new Dictionary<string, string>
                {
                    ["Total Ingested"] = total.ToString(),
                    ["Currently Processing"] = processing.ToString(),
                    ["Failed Versions"] = failed.ToString(),
                    ["Pipeline Health"] = failed == 0 ? "Optimal" : "Attention Required"
                },
                ConfirmButtonText: "View Ingestion Queue",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

