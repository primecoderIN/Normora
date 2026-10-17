using System.Text.Json;

namespace Normora.Api.Features.Ask.Tools;

/// <summary>
/// Deterministically calculates corporate expense limits and reimbursement caps based on standard corporate policy tiers.
/// </summary>
public sealed class CalculatePolicyLimitTool : IAgentTool
{
    public string Name => "calculate_policy_limit";

    public string Description =>
        "Calculates standard corporate expense limits, travel per-diem allowances, hotel night caps, meal allowances, and equipment stipends.";

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            policyType = new
            {
                type = "string",
                description = "Category of expense: 'hotel', 'per_diem', 'meal', 'hardware', or 'wellness'."
            },
            tierOrLocation = new
            {
                type = "string",
                description = "Location tier or region: 'tier_1' (high-cost metro like NY/SF/London), 'tier_2' (standard metro), 'tier_3' (regional/rural), or 'international'."
            },
            days = new
            {
                type = "integer",
                description = "Number of travel days or hotel nights (defaults to 1)."
            }
        },
        required = new[] { "policyType" }
    };

    public Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            var policyType = root.TryGetProperty("policyType", out var pt)
                ? pt.GetString()?.Trim().ToLowerInvariant()
                : "per_diem";

            var tier = root.TryGetProperty("tierOrLocation", out var tl)
                ? tl.GetString()?.Trim().ToLowerInvariant()
                : "tier_2";

            var days = root.TryGetProperty("days", out var d) && d.TryGetInt32(out var parsedDays)
                ? Math.Max(1, parsedDays)
                : 1;

            object result = policyType switch
            {
                "hotel" => CalculateHotel(tier, days),
                "per_diem" => CalculatePerDiem(tier, days),
                "meal" => CalculateMeal(tier, days),
                "hardware" => CalculateHardware(),
                "wellness" => CalculateWellness(),
                _ => new
                {
                    policy = policyType,
                    message = $"Unknown policy type '{policyType}'. Supported policies: hotel, per_diem, meal, hardware, wellness."
                }
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(new AgentToolResult(true, outputJson));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AgentToolResult(false, string.Empty, $"Failed to calculate policy limit: {ex.Message}"));
        }
    }

    private static object CalculateHotel(string? tier, int nights)
    {
        var (nightlyRate, tierName) = tier switch
        {
            "tier_1" or "high" or "nyc" or "sf" => (275m, "Tier 1 (High-Cost Metro)"),
            "tier_3" or "low" or "regional" => (150m, "Tier 3 (Regional / Standard)"),
            "international" or "intl" => (320m, "International"),
            _ => (200m, "Tier 2 (Standard Metro)")
        };

        var total = nightlyRate * nights;
        return new
        {
            policy = "Hotel Lodging Allowance",
            tier = tierName,
            nights,
            nightlyCapUsd = nightlyRate,
            totalMaxAllowanceUsd = total,
            receiptRequired = true,
            guideline = "Excludes incidentals and hotel room service charges. Must book within preferred corporate rates when available."
        };
    }

    private static object CalculatePerDiem(string? tier, int days)
    {
        var (dailyRate, tierName) = tier switch
        {
            "tier_1" or "high" => (85m, "Tier 1 (High-Cost Metro)"),
            "tier_3" or "low" => (50m, "Tier 3 (Regional)"),
            "international" or "intl" => (110m, "International"),
            _ => (65m, "Tier 2 (Standard Metro)")
        };

        var total = dailyRate * days;
        return new
        {
            policy = "Travel Per-Diem Allowance",
            tier = tierName,
            days,
            dailyRateUsd = dailyRate,
            totalAllowanceUsd = total,
            receiptRequired = false,
            guideline = "Standard per-diem covers daily incidentals and meals. No individual receipts required up to this amount."
        };
    }

    private static object CalculateMeal(string? tier, int days)
    {
        var breakfast = 20m;
        var lunch = 30m;
        var dinner = 45m;
        var dailyMax = 85m;

        return new
        {
            policy = "Single-Day Meal Cap",
            days,
            itemizedLimitsUsd = new
            {
                breakfast,
                lunch,
                dinner
            },
            dailyAggregateCapUsd = dailyMax,
            totalAllowanceUsd = dailyMax * days,
            receiptRequired = true,
            guideline = "Itemized receipts with merchant details required. Alcohol excluded unless pre-approved for client entertainment."
        };
    }

    private static object CalculateHardware() => new
    {
        policy = "Hardware & Remote Equipment Stipend",
        laptopBudgetUsd = 1200m,
        laptopRefreshCadence = "Every 24 months",
        peripheralStipendUsd = 500m,
        peripheralCadence = "Annual",
        allowedItems = new[] { "External monitors (up to 4K)", "Ergonomic keyboard/mouse", "Docking station", "Noise-cancelling headset", "Webcam" },
        guideline = "Hardware remains company property until standard depreciation period completes."
    };

    private static object CalculateWellness() => new
    {
        policy = "Health & Wellness Subsidy",
        monthlyAllowanceUsd = 75m,
        annualMaxUsd = 900m,
        eligibleCategories = new[] { "Gym memberships", "Fitness equipment", "Mental health & meditation apps", "Marathon registration fees" },
        guideline = "Claim monthly via expense portal with proof of payment."
    };
}
