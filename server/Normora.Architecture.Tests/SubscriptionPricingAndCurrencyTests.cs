using Normora.Modules.Tenants.Application.Subscription.Commands.UpdateTenantCurrency;
using Normora.Modules.Tenants.Domain;
using Xunit;

namespace Normora.Architecture.Tests;

public class SubscriptionPricingAndCurrencyTests
{
    [Theory]
    [InlineData("INR", "inr")]
    [InlineData("usd", "usd")]
    [InlineData("EUR", "eur")]
    [InlineData("gbp", "gbp")]
    [InlineData("Cad", "cad")]
    [InlineData("AUD", "aud")]
    [InlineData("JPY", "jpy")]
    [InlineData("SGD", "sgd")]
    [InlineData(null, "inr")]
    [InlineData("", "inr")]
    [InlineData("unsupported", "inr")]
    public void NormalizeCurrency_ValidatesAndNormalizesExpectedly(string? input, string expected)
    {
        var result = TenantPlanPricingCatalog.NormalizeCurrency(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FreeTier_AlwaysReturnsZeroPrice()
    {
        foreach (var currency in TenantPlanPricingCatalog.SupportedCurrencies)
        {
            Assert.Equal(0.00m, TenantPlanPricingCatalog.GetPrice(PlanTier.Free, currency, "monthly"));
            Assert.Equal(0.00m, TenantPlanPricingCatalog.GetPrice(PlanTier.Free, currency, "annual"));
            Assert.Equal(0.00m, TenantPlanPricingCatalog.GetMonthlyEquivalentPrice(PlanTier.Free, currency, "monthly"));
        }
    }

    [Theory]
    [InlineData("inr", 3999.00, 38388.00)]
    [InlineData("usd", 49.00, 468.00)]
    [InlineData("eur", 45.00, 432.00)]
    [InlineData("gbp", 39.00, 372.00)]
    [InlineData("cad", 65.00, 624.00)]
    [InlineData("aud", 75.00, 720.00)]
    [InlineData("jpy", 7500.00, 72000.00)]
    [InlineData("sgd", 65.00, 624.00)]
    public void ProTier_PricingMatchesAcrossAllCurrencies(string currency, decimal monthlyPrice, decimal annualPrice)
    {
        var actualMonthly = TenantPlanPricingCatalog.GetPrice(PlanTier.Pro, currency, "monthly");
        var actualAnnual = TenantPlanPricingCatalog.GetPrice(PlanTier.Pro, currency, "annual");

        Assert.Equal(monthlyPrice, actualMonthly);
        Assert.Equal(annualPrice, actualAnnual);
    }

    [Theory]
    [InlineData("inr", 19999.00, 191988.00)]
    [InlineData("usd", 249.00, 2388.00)]
    [InlineData("eur", 229.00, 2196.00)]
    [InlineData("gbp", 199.00, 1908.00)]
    [InlineData("cad", 329.00, 3156.00)]
    [InlineData("aud", 379.00, 3636.00)]
    [InlineData("jpy", 38000.00, 364800.00)]
    [InlineData("sgd", 329.00, 3156.00)]
    public void EnterpriseTier_PricingMatchesAcrossAllCurrencies(string currency, decimal monthlyPrice, decimal annualPrice)
    {
        var actualMonthly = TenantPlanPricingCatalog.GetPrice(PlanTier.Enterprise, currency, "monthly");
        var actualAnnual = TenantPlanPricingCatalog.GetPrice(PlanTier.Enterprise, currency, "annual");

        Assert.Equal(monthlyPrice, actualMonthly);
        Assert.Equal(annualPrice, actualAnnual);
    }

    [Theory]
    [InlineData("inr", "₹")]
    [InlineData("usd", "$")]
    [InlineData("eur", "€")]
    [InlineData("gbp", "£")]
    [InlineData("cad", "C$")]
    [InlineData("aud", "A$")]
    [InlineData("jpy", "¥")]
    [InlineData("sgd", "S$")]
    public void CurrencySymbols_AreMappedCorrectly(string currency, string expectedSymbol)
    {
        var symbol = TenantPlanPricingCatalog.GetCurrencySymbol(currency);
        Assert.Equal(expectedSymbol, symbol);
    }

    [Fact]
    public void UpdateTenantCurrencyCommandValidator_ValidatesCurrencyInput()
    {
        var validator = new UpdateTenantCurrencyCommandValidator();

        var validResult = validator.Validate(new UpdateTenantCurrencyCommand("USD"));
        Assert.True(validResult.IsValid);

        var emptyResult = validator.Validate(new UpdateTenantCurrencyCommand(""));
        Assert.False(emptyResult.IsValid);

        var unsupportedResult = validator.Validate(new UpdateTenantCurrencyCommand("BITCOIN"));
        Assert.False(unsupportedResult.IsValid);
    }

    [Fact]
    public void DefaultFreePlan_HasCleanInitialStateWithoutPaidSubscriptionArtifacts()
    {
        var tenantId = Guid.NewGuid();
        var freePlan = TenantPlan.CreateDefault(tenantId, PlanTier.Free, "usd");

        Assert.Equal(PlanTier.Free, freePlan.PlanTier);
        Assert.Equal("usd", freePlan.Currency);
        Assert.Equal("free", freePlan.SubscriptionStatus);
        Assert.False(freePlan.CancelAtPeriodEnd);
        Assert.False(freePlan.AutoRenewEnabled);
        Assert.Null(freePlan.CurrentPeriodEndUtc);
        Assert.Null(freePlan.PaymentTransactionId);
        Assert.Null(freePlan.PaymentReferenceId);
        Assert.Null(freePlan.MandateToken);
    }

    [Fact]
    public void SubscriptionConstants_ContainFreePlanGuardMessages()
    {
        Assert.Equal("Free plans do not have an active subscription to cancel.", Normora.Shared.Constants.ApiMessages.CannotCancelFreePlan);
        Assert.Equal("Free plans do not have an active subscription to reactivate.", Normora.Shared.Constants.ApiMessages.CannotReactivateFreePlan);
    }
}
