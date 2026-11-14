using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for document catalog listing and deletion results.
/// </summary>
public sealed class DocumentsListCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        toolName is "documents_list" or "documents_delete";

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

            if (toolName == "documents_delete")
            {
                var docId = root.TryGetProperty("documentId", out var idProp) ? idProp.GetString() : "Document";

                return new ActionCardItem(
                    ActionType: "document_deleted_card",
                    Title: "Document Soft-Deleted",
                    Description: "Document removed from search. Retained for 30 days before permanent purge.",
                    Parameters: new Dictionary<string, string>
                    {
                        ["Document ID"] = docId ?? "",
                        ["Retention Grace Period"] = "30 Days",
                        ["Status"] = "Soft-Deleted"
                    },
                    ConfirmButtonText: "Open Document Vault",
                    CancelButtonText: "Dismiss");
            }

            var total = root.TryGetProperty("totalDocuments", out var t) ? t.GetInt32() : 0;
            var parameters = new Dictionary<string, string>
            {
                ["Total Catalog Volume"] = total.ToString()
            };

            if (root.TryGetProperty("documents", out var docsArray) && docsArray.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var d in docsArray.EnumerateArray().Take(3))
                {
                    var fn = d.TryGetProperty("fileName", out var f) ? f.GetString() : "File";
                    var status = d.TryGetProperty("status", out var s) ? s.GetString() : "Indexed";
                    parameters[$"File {index++}"] = $"{fn} [{status}]";
                }
            }

            return new ActionCardItem(
                ActionType: "documents_catalog_card",
                Title: "Knowledge Base Documents",
                Description: $"Organization catalog holds {total} active document(s).",
                Parameters: parameters,
                ConfirmButtonText: "Manage Documents",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

