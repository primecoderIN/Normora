using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Action card builder for the destructive <c>documents_delete</c> tool.
/// Surfaces document identification, grace period, and approval confirmation buttons.
/// </summary>
public sealed class DocumentsDeleteCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) =>
        string.Equals(toolName, "documents_delete", StringComparison.OrdinalIgnoreCase);

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        var documentId = "Unknown";
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            if (doc.RootElement.TryGetProperty("documentId", out var idProp))
            {
                documentId = idProp.GetString() ?? "Unknown";
            }
        }
        catch
        {
            // fallback to default display
        }

        var parameters = new Dictionary<string, string>
        {
            ["Target Document ID"] = documentId,
            ["Grace Period"] = "30 Days (Soft-Delete)",
            ["Action Type"] = "Knowledge Base Removal"
        };

        return new ActionCardItem(
            ActionType: "documents_delete_card",
            Title: "Authorization Required: Delete Document",
            Description: "Soft-deletes this document from the enterprise index with a 30-day recovery grace period.",
            Parameters: parameters,
            ConfirmButtonText: "Confirm & Delete",
            CancelButtonText: "Keep Document");
    }
}

