using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card for workspace subscription usage, plan quotas, and renewal details.
/// </summary>
public sealed class WorkspaceSubscriptionCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) => toolName == "workspace_get_subscription_usage";

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(result.Output);
            var root = doc.RootElement;

            var tier = root.TryGetProperty("planTier", out var tProp) ? tProp.GetString() : "Free";
            var docsUsed = root.TryGetProperty("documentsUsed", out var du) ? du.GetInt32() : 0;
            var maxDocs = root.TryGetProperty("maxDocuments", out var md) ? md.GetInt32() : 0;
            var empsUsed = root.TryGetProperty("employeesUsed", out var eu) ? eu.GetInt32() : 0;
            var maxEmps = root.TryGetProperty("maxEmployees", out var me) ? me.GetInt32() : 0;
            var storageBytes = root.TryGetProperty("storageBytesUsed", out var sb) ? sb.GetInt64() : 0;
            var maxStorage = root.TryGetProperty("maxStorageBytes", out var ms) ? ms.GetInt64() : 0;
            var status = root.TryGetProperty("subscriptionStatus", out var sProp) ? sProp.GetString() : "active";

            var storageMbUsed = (storageBytes / (1024.0 * 1024.0)).ToString("F1") + " MB";
            var storageMbMax = (maxStorage / (1024.0 * 1024.0)).ToString("F0") + " MB";

            return new ActionCardItem(
                ActionType: "subscription_usage_card",
                Title: $"{tier} Plan Utilization",
                Description: "Current consumption against allocated organization resource limits.",
                Parameters: new Dictionary<string, string>
                {
                    ["Active Plan"] = $"{tier} ({status?.ToUpperInvariant()})",
                    ["Document Capacity"] = $"{docsUsed} / {maxDocs} documents",
                    ["Employee Seats"] = $"{empsUsed} / {maxEmps} seats",
                    ["Storage Consumed"] = $"{storageMbUsed} / {storageMbMax}",
                    ["route"] = "/employer/settings/subscription"
                },
                ConfirmButtonText: "Manage Subscription",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}
