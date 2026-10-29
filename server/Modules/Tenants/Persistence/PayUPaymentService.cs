using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Modules.Tenants.Application.Subscription;
using Normora.Modules.Tenants.Domain;
using Normora.Shared.Constants;
using Normora.Modules.Tenants.Options;
using Normora.Shared.Options;

namespace Normora.Modules.Tenants.Persistence;

public class PayUPaymentService(
    TenantsDbContext context,
    IOptions<PayUOptions> payUOptions,
    IOptions<AppOptions> appOptions,
    ILogger<PayUPaymentService> logger) : IPaymentService, IPayUPaymentService
{
    private readonly PayUOptions _options = payUOptions.Value;
    private readonly AppOptions _app = appOptions.Value;

    public async Task<PaymentCheckoutDto> CreatePaymentSessionAsync(
        Guid tenantId,
        string tenantName,
        string tenantSlug,
        string userEmail,
        PlanTier tier,
        string? currency = "inr",
        CancellationToken cancellationToken = default)
    {
        if (tier == PlanTier.Free)
        {
            throw new InvalidOperationException(ApiMessages.FreeTierCannotCheckout);
        }

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? (!string.IsNullOrWhiteSpace(plan?.Currency) ? plan.Currency : "inr")
            : currency.Trim().ToLowerInvariant();

        // Calculate tier price
        var amount = (tier, normalizedCurrency) switch
        {
            (PlanTier.Pro, "inr") => 3999.00m,
            (PlanTier.Enterprise, "inr") => 19999.00m,

            (PlanTier.Pro, "usd") => 49.00m,
            (PlanTier.Enterprise, "usd") => 249.00m,

            (PlanTier.Pro, "eur") => 45.00m,
            (PlanTier.Enterprise, "eur") => 229.00m,

            (PlanTier.Pro, "gbp") => 39.00m,
            (PlanTier.Enterprise, "gbp") => 199.00m,

            (PlanTier.Pro, "cad") => 65.00m,
            (PlanTier.Enterprise, "cad") => 329.00m,

            (PlanTier.Pro, "aud") => 75.00m,
            (PlanTier.Enterprise, "aud") => 379.00m,

            (PlanTier.Pro, "jpy") => 7500.00m,
            (PlanTier.Enterprise, "jpy") => 38000.00m,

            (PlanTier.Pro, "sgd") => 65.00m,
            (PlanTier.Enterprise, "sgd") => 329.00m,

            _ => tier == PlanTier.Pro ? 3999.00m : 19999.00m
        };

        var amountString = amount.ToString("F2", CultureInfo.InvariantCulture);
        var txnid = "txn_" + Guid.NewGuid().ToString("N")[..18];
        var productInfo = $"Normora {tier} Workspace";
        var safeFirstName = string.IsNullOrWhiteSpace(tenantName) ? "Workspace" : tenantName.Trim();
        var safeEmail = string.IsNullOrWhiteSpace(userEmail) ? "billing@normora.internal" : userEmail.Trim();
        var phone = "9999999999";

        // PayU User Defined Fields (UDF) for state preservation
        var udf1 = tenantId.ToString();
        var udf2 = tier.ToString();
        var udf3 = normalizedCurrency;
        var udf4 = tenantSlug;
        var udf5 = safeEmail;

        var callbackUrl = $"{_app.BaseUrl}/api/subscription/payu-callback";

        // Generate SHA-512 request hash:
        // sha512(key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5||||||SALT)
        var hashSequence = $"{_options.MerchantKey}|{txnid}|{amountString}|{productInfo}|{safeFirstName}|{safeEmail}|{udf1}|{udf2}|{udf3}|{udf4}|{udf5}||||||{_options.MerchantSalt}";
        var hash = ComputeSha512(hashSequence);

        var paramDict = new Dictionary<string, string>
        {
            ["key"] = _options.MerchantKey,
            ["txnid"] = txnid,
            ["amount"] = amountString,
            ["productinfo"] = productInfo,
            ["firstname"] = safeFirstName,
            ["email"] = safeEmail,
            ["phone"] = phone,
            ["surl"] = callbackUrl,
            ["furl"] = callbackUrl,
            ["hash"] = hash,
            ["udf1"] = udf1,
            ["udf2"] = udf2,
            ["udf3"] = udf3,
            ["udf4"] = udf4,
            ["udf5"] = udf5
        };

        logger.LogInformation("Generated PayU checkout session for Tenant {TenantId}, Tier {Tier}, TxnId {TxnId}, Amount {Amount} {Currency}",
            tenantId, tier, txnid, amountString, normalizedCurrency);

        return new PaymentCheckoutDto(_options.PaymentUrl, paramDict);
    }

    public async Task<PaymentCallbackResultDto> ProcessCallbackAsync(
        IFormCollection form,
        CancellationToken cancellationToken = default)
    {
        var status = form["status"].ToString();
        var txnid = form["txnid"].ToString();
        var amount = form["amount"].ToString();
        var productInfo = form["productinfo"].ToString();
        var firstname = form["firstname"].ToString();
        var email = form["email"].ToString();
        var receivedHash = form["hash"].ToString();
        var additionalCharges = form["additionalCharges"].ToString();
        var payuMoneyId = form["payuMoneyId"].ToString();

        var udf1 = form["udf1"].ToString(); // tenantId
        var udf2 = form["udf2"].ToString(); // tier
        var udf3 = form["udf3"].ToString(); // currency
        var udf4 = form["udf4"].ToString(); // tenantSlug
        var udf5 = form["udf5"].ToString(); // userEmail

        if (!Guid.TryParse(udf1, out var tenantId))
        {
            logger.LogWarning("PayU callback received without valid tenantId in udf1: {Udf1}", SanitizeLogValue(udf1));
            return new PaymentCallbackResultDto(false, Guid.Empty, string.Empty, PlanTier.Free, txnid, ApiMessages.NotFound);
        }

        if (!Enum.TryParse<PlanTier>(udf2, true, out var tier))
        {
            tier = PlanTier.Pro;
        }

        // Validate reverse SHA-512 hash:
        // With additionalCharges:
        // sha512(additionalCharges|SALT|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key)
        // Without additionalCharges:
        // sha512(SALT|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key)
        string expectedHashSequence;
        if (!string.IsNullOrWhiteSpace(additionalCharges))
        {
            expectedHashSequence = $"{additionalCharges}|{_options.MerchantSalt}|{status}||||||{udf5}|{udf4}|{udf3}|{udf2}|{udf1}|{email}|{firstname}|{productInfo}|{amount}|{txnid}|{_options.MerchantKey}";
        }
        else
        {
            expectedHashSequence = $"{_options.MerchantSalt}|{status}||||||{udf5}|{udf4}|{udf3}|{udf2}|{udf1}|{email}|{firstname}|{productInfo}|{amount}|{txnid}|{_options.MerchantKey}";
        }

        var calculatedHash = ComputeSha512(expectedHashSequence);
        var isHashValid = !string.IsNullOrEmpty(receivedHash) &&
                          CryptographicOperations.FixedTimeEquals(
                              Encoding.UTF8.GetBytes(calculatedHash.ToLowerInvariant()),
                              Encoding.UTF8.GetBytes(receivedHash.ToLowerInvariant()));

        if (!isHashValid)
        {
            logger.LogWarning("PayU callback signature verification failed for Txn {TxnId}.", SanitizeLogValue(txnid));

            return new PaymentCallbackResultDto(false, tenantId, string.Empty, tier, txnid, ApiMessages.PaymentSignatureMismatch);
        }

        var tenantSlug = await context.Tenants
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.Slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantSlug is null)
        {
            return new PaymentCallbackResultDto(false, tenantId, string.Empty, tier, txnid, ApiMessages.NotFound);
        }

        if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = form["error_Message"].ToString();
            logger.LogWarning("PayU transaction {TxnId} was not successful. Status: {Status}, Error: {Error}",
                SanitizeLogValue(txnid), SanitizeLogValue(status), SanitizeLogValue(errorMessage));
            return new PaymentCallbackResultDto(false, tenantId, tenantSlug, tier, txnid, string.IsNullOrWhiteSpace(errorMessage) ? ApiMessages.PaymentProcessingFailed : errorMessage);
        }

        // Fulfillment: update TenantPlan in database
        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var template = TenantPlan.CreateDefault(tenantId, tier, udf3);

        if (plan == null)
        {
            plan = template;
            plan.PaymentTransactionId = txnid;
            plan.PaymentReferenceId = payuMoneyId;
            plan.SubscriptionStatus = "active";
            plan.Currency = string.IsNullOrWhiteSpace(udf3) ? "inr" : udf3.ToLowerInvariant();
            plan.CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1);
            context.TenantPlans.Add(plan);
        }
        else
        {
            plan.PlanTier = tier;
            plan.Currency = string.IsNullOrWhiteSpace(udf3) ? plan.Currency : udf3.ToLowerInvariant();
            plan.MaxDocuments = template.MaxDocuments;
            plan.MaxEmployees = template.MaxEmployees;
            plan.MaxStorageBytes = template.MaxStorageBytes;
            plan.PaymentTransactionId = txnid;
            plan.PaymentReferenceId = payuMoneyId;
            plan.SubscriptionStatus = "active";
            plan.CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1);
            plan.UpdatedAt = DateTime.UtcNow;
        }

        // Record immutable audit log
        context.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            Action = "Subscription.PayUCheckoutCompleted",
            TargetType = "TenantPlan",
            TargetId = plan.Id.ToString(),
            ActorUserId = "payu_gateway",
            ActorEmail = email,
            ActorName = firstname,
            Metadata = JsonSerializer.Serialize(new
            {
                tier = tier.ToString(),
                txnid,
                amount,
                currency = udf3,
                payuMoneyId,
                status
            })
        });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Tenant {TenantId} successfully upgraded to {Tier} via PayU payment {TxnId}",
            tenantId, tier, SanitizeLogValue(txnid));

        return new PaymentCallbackResultDto(true, tenantId, tenantSlug, tier, txnid, null);
    }

    private static string ComputeSha512(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA512.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    private static string SanitizeLogValue(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
