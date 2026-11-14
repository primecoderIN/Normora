using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card displaying relevant document passages found via vector search.
/// </summary>
public sealed class DocumentsSearchCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "documents_search";

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

            var query = root.TryGetProperty("query", out var q) ? q.GetString() : "Knowledge Search";
            var total = root.TryGetProperty("totalFound", out var t) ? t.GetInt32() : 0;

            var parameters = new Dictionary<string, string>
            {
                ["Query"] = query ?? "Search",
                ["Sections Found"] = total.ToString()
            };

            if (root.TryGetProperty("results", out var resArray) && resArray.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var chunk in resArray.EnumerateArray().Take(3))
                {
                    var fileName = chunk.TryGetProperty("fileName", out var fn) ? fn.GetString() : "Document";
                    var score = chunk.TryGetProperty("similarity", out var s) ? s.GetDouble() : 0.0;
                    parameters[$"Match {index++}"] = $"{fileName} (Score: {score:P0})";
                }
            }

            return new ActionCardItem(
                ActionType: "document_search_card",
                Title: "Knowledge Base Search Results",
                Description: $"Discovered {total} relevant policy excerpt(s) for '{query}'.",
                Parameters: parameters,
                ConfirmButtonText: "View Full Sections",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

