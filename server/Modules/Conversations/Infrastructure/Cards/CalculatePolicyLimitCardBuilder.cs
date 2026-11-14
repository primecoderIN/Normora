using System.Text.Json;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive policy limit calculation card when <c>calculate_policy_limit</c> finishes.
/// </summary>
public sealed class CalculatePolicyLimitCardBuilder : IActionCardBuilder
{
    private const string TargetToolName = "calculate_policy_limit";

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
            var policy = root.TryGetProperty("policy", out var p) ? p.GetString() : "Policy Limit";
            var tier = root.TryGetProperty("tier", out var t) ? t.GetString() : null;
            var guideline = root.TryGetProperty("guideline", out var g) ? g.GetString() : null;

            var parameters = new Dictionary<string, string>();
            if (tier != null) parameters["Location Tier"] = tier;
            if (root.TryGetProperty("totalMaxAllowanceUsd", out var a))
                parameters["Total Max Allowance"] = $"${a.GetDecimal():N0}";
            if (root.TryGetProperty("nightlyCapUsd", out var n))
                parameters["Nightly Cap"] = $"${n.GetDecimal():N0}/night";
            if (root.TryGetProperty("dailyAllowanceUsd", out var d))
                parameters["Daily Allowance"] = $"${d.GetDecimal():N0}/day";
            if (guideline != null) parameters["Policy Guideline"] = guideline;

            return new ActionCardItem(
                ActionType: "policy_limit_card",
                Title: policy ?? "Expense Policy Calculation",
                Description: $"Deterministic corporate limit calculation for {tier ?? "requested expense"}.",
                Parameters: parameters,
                ConfirmButtonText: "Apply Limit",
                CancelButtonText: "Dismiss");
        }
        catch
        {
            return null;
        }
    }
}

