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
using System.Net.Http;
using Normora.Shared.Options;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>Creates hosted PayU payments and verifies callbacks before subscription fulfillment.</summary>
public class PayUPaymentService(
    TenantsDbContext context,
    IOptions<PayUOptions> payUOptions,
    IOptions<AppOptions> appOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<PayUPaymentService> logger) : IPaymentService
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
        string? billingCycle = "monthly",
        bool enableAutoRenew = true,
        CancellationToken cancellationToken = default)
    {
        if (tier == PlanTier.Free)
        {
            throw new InvalidOperationException(ApiMessages.FreeTierCannotCheckout);
        }

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var normalizedCurrency = TenantPlanPricingCatalog.NormalizeCurrency(
            string.IsNullOrWhiteSpace(currency) ? plan?.Currency : currency);

        var normalizedCycle = string.Equals(billingCycle, "annual", StringComparison.OrdinalIgnoreCase)
            ? "annual"
            : "monthly";

        // Calculate tier price using authoritative domain pricing catalog
        var amount = TenantPlanPricingCatalog.GetPrice(tier, normalizedCurrency, normalizedCycle);

        var amountString = amount.ToString("F2", CultureInfo.InvariantCulture);
        var txnid = "txn_" + Guid.NewGuid().ToString("N")[..18];
        var cycleLabel = normalizedCycle == "annual" ? "Annual" : "Monthly";
        var productInfo = $"Normora {tier} Workspace ({cycleLabel})";
        var safeFirstName = string.IsNullOrWhiteSpace(tenantName) ? "Workspace" : tenantName.Trim();
        var safeEmail = string.IsNullOrWhiteSpace(userEmail) ? "billing@normora.internal" : userEmail.Trim();
        var phone = "9999999999";

        // PayU User Defined Fields (UDF) for state preservation
        var udf1 = tenantId.ToString();
        var udf2 = tier.ToString();
        var udf3 = $"{normalizedCurrency}:{normalizedCycle}";
        var udf4 = tenantSlug;
        var udf5 = safeEmail;

        var callbackUrl = $"{_app.BaseUrl}/api/subscription/payu-callback";

        string? siDetails = null;
        if (enableAutoRenew)
        {
            var billingCycleEnum = normalizedCycle == "annual" ? "YEARLY" : "MONTHLY";
            siDetails = JsonSerializer.Serialize(new
            {
                billingAmount = amountString,
                billingCurrency = normalizedCurrency.ToUpperInvariant(),
                billingCycle = billingCycleEnum,
                billingInterval = 1,
                paymentStartDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                paymentEndDate = DateTime.UtcNow.AddYears(5).ToString("yyyy-MM-dd")
            });
        }

        // Generate SHA-512 request hash:
        // With Standing Instruction (SI / UPI AutoPay):
        // sha512(key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5||||||si_details|SALT)
        // Without SI:
        // sha512(key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5||||||SALT)
        var hashSequence = enableAutoRenew && siDetails != null
            ? $"{_options.MerchantKey}|{txnid}|{amountString}|{productInfo}|{safeFirstName}|{safeEmail}|{udf1}|{udf2}|{udf3}|{udf4}|{udf5}||||||{siDetails}|{_options.MerchantSalt}"
            : $"{_options.MerchantKey}|{txnid}|{amountString}|{productInfo}|{safeFirstName}|{safeEmail}|{udf1}|{udf2}|{udf3}|{udf4}|{udf5}||||||{_options.MerchantSalt}";
        var hash = ComputeSha512(hashSequence);

        var paramDict = new Dictionary<string, string>
        {
            ["key"] = _options.MerchantKey,
            ["txnid"] = txnid,
            ["amount"] = amountString,
            ["currency"] = normalizedCurrency.ToUpperInvariant(),
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

        if (enableAutoRenew && siDetails != null)
        {
            paramDict["si"] = "1";
            paramDict["api_version"] = "7";
            paramDict["si_details"] = siDetails;
        }

        logger.LogInformation("Generated PayU checkout session for Tenant {TenantId}, Tier {Tier}, Cycle {Cycle}, AutoRenew {AutoRenew}, TxnId {TxnId}, Amount {Amount} {Currency}",
            tenantId, tier, normalizedCycle, enableAutoRenew, txnid, amountString, normalizedCurrency.ToUpperInvariant());

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

        // Parse UDF3 containing normalized currency and billing cycle (e.g. "usd:annual" or legacy "usd")
        var udf3Parts = udf3.Split(':');
        var callbackCurrency = TenantPlanPricingCatalog.NormalizeCurrency(udf3Parts.Length > 0 ? udf3Parts[0] : "inr");
        var callbackCycle = udf3Parts.Length > 1 && string.Equals(udf3Parts[1], "annual", StringComparison.OrdinalIgnoreCase)
            ? "annual"
            : "monthly";
        var isAnnual = callbackCycle == "annual";

        // Amount verification: ensure the paid amount matches the authoritative domain pricing catalog
        var expectedAmount = TenantPlanPricingCatalog.GetPrice(tier, callbackCurrency, callbackCycle);
        if (!decimal.TryParse(amount, CultureInfo.InvariantCulture, out var parsedAmount) || Math.Abs(parsedAmount - expectedAmount) > 0.01m)
        {
            logger.LogWarning("PayU transaction {TxnId} amount mismatch. Expected: {Expected}, Received: {Received}",
                SanitizeLogValue(txnid), expectedAmount, SanitizeLogValue(amount));
            return new PaymentCallbackResultDto(false, tenantId, tenantSlug, tier, txnid, ApiMessages.PaymentProcessingFailed);
        }

        // Fulfillment: update TenantPlan in database
        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        // Idempotency check: if this transaction was already processed, avoid duplicate renewals or audit entries
        if (plan != null && string.Equals(plan.PaymentTransactionId, txnid, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("PayU transaction {TxnId} was already fulfilled for Tenant {TenantId}. Returning existing confirmation.",
                SanitizeLogValue(txnid), tenantId);
            return new PaymentCallbackResultDto(true, tenantId, tenantSlug, tier, txnid, null);
        }

        // Extract mandate tokens if a recurring Standing Instruction / UPI AutoPay was registered
        var mihpayid = form["mihpayid"].ToString();
        var authpayuid = form["authpayuid"].ToString();
        var isStandingInstruction = string.Equals(form["IsStandingInstructionSet"].ToString(), "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(form["si"].ToString(), "1", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(authpayuid);

        var mandateToken = !string.IsNullOrWhiteSpace(authpayuid)
            ? authpayuid
            : (!string.IsNullOrWhiteSpace(mihpayid) ? mihpayid : null);

        var template = TenantPlan.CreateDefault(tenantId, tier, callbackCurrency);

        if (plan == null)
        {
            plan = template;
            plan.PaymentTransactionId = txnid;
            plan.PaymentReferenceId = payuMoneyId;
            plan.SubscriptionStatus = "active";
            plan.Currency = callbackCurrency;
            plan.CurrentPeriodEndUtc = isAnnual ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
            if (isStandingInstruction && !string.IsNullOrWhiteSpace(mandateToken))
            {
                plan.MandateToken = mandateToken;
                plan.AutoRenewEnabled = true;
            }
            context.TenantPlans.Add(plan);
        }
        else
        {
            plan.PlanTier = tier;
            plan.Currency = callbackCurrency;
            plan.MaxDocuments = template.MaxDocuments;
            plan.MaxEmployees = template.MaxEmployees;
            plan.MaxStorageBytes = template.MaxStorageBytes;
            plan.PaymentTransactionId = txnid;
            plan.PaymentReferenceId = payuMoneyId;
            plan.SubscriptionStatus = "active";
            plan.CurrentPeriodEndUtc = isAnnual ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
            if (isStandingInstruction && !string.IsNullOrWhiteSpace(mandateToken))
            {
                plan.MandateToken = mandateToken;
                plan.AutoRenewEnabled = true;
            }
            plan.UpdatedAt = DateTime.UtcNow;
        }

        // Record itemized paid invoice
        context.TenantInvoices.Add(new TenantInvoice
        {
            TenantId = tenantId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{txnid[..Math.Min(txnid.Length, 8)]}",
            InvoiceDate = DateTime.UtcNow,
            PlanTier = tier.ToString(),
            Amount = parsedAmount,
            Currency = callbackCurrency.ToUpperInvariant(),
            BillingCycle = isAnnual ? "Annual" : "Monthly",
            Status = "Paid",
            PaymentTransactionId = txnid,
            CustomerName = firstname,
            CustomerEmail = email
        });

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
                currency = callbackCurrency,
                billingCycle = callbackCycle,
                payuMoneyId,
                status
            })
        });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Tenant {TenantId} successfully upgraded to {Tier} ({Cycle}) via PayU payment {TxnId}",
            tenantId, tier, callbackCycle, SanitizeLogValue(txnid));

        return new PaymentCallbackResultDto(true, tenantId, tenantSlug, tier, txnid, null, plan.MandateToken, plan.AutoRenewEnabled);
    }

    public async Task<RecurringPaymentResultDto> ExecuteRecurringPaymentAsync(
        Guid tenantId,
        decimal amount,
        string mandateToken,
        PlanTier tier,
        string currency,
        string userEmail,
        string customerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mandateToken))
        {
            return new RecurringPaymentResultDto(false, string.Empty, amount, "Mandate token is missing.");
        }

        var amountString = amount.ToString("F2", CultureInfo.InvariantCulture);
        var txnid = "rec_" + Guid.NewGuid().ToString("N")[..18];
        var invoiceNo = $"INV-{DateTime.UtcNow:yyyyMM}-{txnid[..Math.Min(txnid.Length, 8)]}";
        var safeEmail = string.IsNullOrWhiteSpace(userEmail) ? "billing@normora.internal" : userEmail.Trim();

        var var1Dict = new Dictionary<string, string>
        {
            ["authpayuid"] = mandateToken,
            ["amount"] = amountString,
            ["txnid"] = txnid,
            ["phone"] = "9999999999",
            ["email"] = safeEmail,
            ["invoiceDisplayNumber"] = invoiceNo
        };

        var var1Json = JsonSerializer.Serialize(var1Dict);
        // PayU si_transaction command hash: sha512(key|command|var1|salt)
        var hashSequence = $"{_options.MerchantKey}|si_transaction|{var1Json}|{_options.MerchantSalt}";
        var hash = ComputeSha512(hashSequence);

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("form", "2"),
            new KeyValuePair<string, string>("key", _options.MerchantKey),
            new KeyValuePair<string, string>("command", "si_transaction"),
            new KeyValuePair<string, string>("var1", var1Json),
            new KeyValuePair<string, string>("hash", hash)
        });

        var client = httpClientFactory.CreateClient("PayU");
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            var response = await client.PostAsync(_options.PostServiceUrl, formContent, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogInformation("PayU recurring debit attempt for Tenant {TenantId}, TxnId {TxnId}, HTTP {StatusCode}",
                tenantId, txnid, response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PayU recurring debit returned HTTP error {StatusCode} for Tenant {TenantId}",
                    response.StatusCode, tenantId);
                return new RecurringPaymentResultDto(false, txnid, amount, $"PayU HTTP {response.StatusCode}");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetInt32() : 0;
                var msg = root.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() : null;

                if (status == 1 || string.Equals(msg, "Transaction Successful", StringComparison.OrdinalIgnoreCase))
                {
                    return new RecurringPaymentResultDto(true, txnid, amount, null);
                }

                return new RecurringPaymentResultDto(false, txnid, amount, msg ?? ApiMessages.PaymentProcessingFailed);
            }
            catch (JsonException)
            {
                if (response.IsSuccessStatusCode && responseString.Contains("success", StringComparison.OrdinalIgnoreCase))
                {
                    return new RecurringPaymentResultDto(true, txnid, amount, null);
                }

                return new RecurringPaymentResultDto(false, txnid, amount, ApiMessages.PaymentProcessingFailed);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PayU recurring debit exception for Tenant {TenantId}, TxnId {TxnId}", tenantId, txnid);
            return new RecurringPaymentResultDto(false, txnid, amount, ApiMessages.PaymentProcessingFailed);
        }
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
