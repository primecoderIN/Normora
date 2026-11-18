using System.Globalization;

namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Authoritative domain catalog for subscription tier pricing, currency conversions,
/// and billing cycle schedules across Normora SaaS workspaces.
/// </summary>
public static class TenantPlanPricingCatalog
{
    public static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "inr",
        "usd",
        "eur",
        "gbp",
        "cad",
        "aud",
        "jpy",
        "sgd"
    };

    public static string NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return "inr";
        var trimmed = currency.Trim().ToLowerInvariant();
        return SupportedCurrencies.Contains(trimmed) ? trimmed : "inr";
    }

    public static bool IsSupportedCurrency(string currency) =>
        !string.IsNullOrWhiteSpace(currency) && SupportedCurrencies.Contains(currency.Trim());

    public static string GetCurrencySymbol(string currency) =>
        NormalizeCurrency(currency) switch
        {
            "usd" => "$",
            "eur" => "€",
            "gbp" => "£",
            "cad" => "C$",
            "aud" => "A$",
            "jpy" => "¥",
            "sgd" => "S$",
            _ => "₹"
        };

    /// <summary>
    /// Computes the exact total billable transaction amount charged by the payment gateway for the specified tier,
    /// currency, and billing cycle (Monthly vs Annual - 20% discount).
    /// </summary>
    public static decimal GetPrice(PlanTier tier, string? currency, string? billingCycle = "monthly")
    {
        if (tier == PlanTier.Free) return 0.00m;

        var curr = NormalizeCurrency(currency);
        var isAnnual = string.Equals(billingCycle, "annual", StringComparison.OrdinalIgnoreCase);

        return (tier, curr, isAnnual) switch
        {
            // PRO TIER
            (PlanTier.Pro, "inr", false) => 3999.00m,
            (PlanTier.Pro, "inr", true) => 38388.00m, // 3,199/mo * 12

            (PlanTier.Pro, "usd", false) => 49.00m,
            (PlanTier.Pro, "usd", true) => 468.00m, // 39/mo * 12

            (PlanTier.Pro, "eur", false) => 45.00m,
            (PlanTier.Pro, "eur", true) => 432.00m, // 36/mo * 12

            (PlanTier.Pro, "gbp", false) => 39.00m,
            (PlanTier.Pro, "gbp", true) => 372.00m, // 31/mo * 12

            (PlanTier.Pro, "cad", false) => 65.00m,
            (PlanTier.Pro, "cad", true) => 624.00m, // 52/mo * 12

            (PlanTier.Pro, "aud", false) => 75.00m,
            (PlanTier.Pro, "aud", true) => 720.00m, // 60/mo * 12

            (PlanTier.Pro, "jpy", false) => 7500.00m,
            (PlanTier.Pro, "jpy", true) => 72000.00m, // 6,000/mo * 12

            (PlanTier.Pro, "sgd", false) => 65.00m,
            (PlanTier.Pro, "sgd", true) => 624.00m, // 52/mo * 12

            // ENTERPRISE TIER
            (PlanTier.Enterprise, "inr", false) => 19999.00m,
            (PlanTier.Enterprise, "inr", true) => 191988.00m, // 15,999/mo * 12

            (PlanTier.Enterprise, "usd", false) => 249.00m,
            (PlanTier.Enterprise, "usd", true) => 2388.00m, // 199/mo * 12

            (PlanTier.Enterprise, "eur", false) => 229.00m,
            (PlanTier.Enterprise, "eur", true) => 2196.00m, // 183/mo * 12

            (PlanTier.Enterprise, "gbp", false) => 199.00m,
            (PlanTier.Enterprise, "gbp", true) => 1908.00m, // 159/mo * 12

            (PlanTier.Enterprise, "cad", false) => 329.00m,
            (PlanTier.Enterprise, "cad", true) => 3156.00m, // 263/mo * 12

            (PlanTier.Enterprise, "aud", false) => 379.00m,
            (PlanTier.Enterprise, "aud", true) => 3636.00m, // 303/mo * 12

            (PlanTier.Enterprise, "jpy", false) => 38000.00m,
            (PlanTier.Enterprise, "jpy", true) => 364800.00m, // 30,400/mo * 12

            (PlanTier.Enterprise, "sgd", false) => 329.00m,
            (PlanTier.Enterprise, "sgd", true) => 3156.00m, // 263/mo * 12

            _ => tier == PlanTier.Pro ? (isAnnual ? 38388.00m : 3999.00m) : (isAnnual ? 191988.00m : 19999.00m)
        };
    }

    /// <summary>
    /// Computes the effective per-month rate for UI representation (e.g. $39/mo vs $49/mo).
    /// </summary>
    public static decimal GetMonthlyEquivalentPrice(PlanTier tier, string? currency, string? billingCycle = "monthly")
    {
        if (tier == PlanTier.Free) return 0.00m;

        var isAnnual = string.Equals(billingCycle, "annual", StringComparison.OrdinalIgnoreCase);
        if (!isAnnual) return GetPrice(tier, currency, "monthly");

        var totalPrice = GetPrice(tier, currency, "annual");
        return Math.Round(totalPrice / 12m, 2);
    }

    public static string FormatPrice(decimal amount, string currency)
    {
        var sym = GetCurrencySymbol(currency);
        var curr = NormalizeCurrency(currency);
        return curr switch
        {
            "jpy" => $"{sym}{amount:N0}",
            _ => $"{sym}{amount:N0}"
        };
    }
}
