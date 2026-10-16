using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Modules.Tenants.Application.Subscription;
using Normora.Modules.Tenants.Domain;
using Normora.Shared.Constants;
using Normora.Shared.Options;
using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;
using PlanTier = Normora.Modules.Tenants.Domain.PlanTier;

namespace Normora.Modules.Tenants.Persistence;

public class StripePaymentService(
    TenantsDbContext context,
    IOptions<StripeOptions> stripeOptions,
    IOptions<AppOptions> appOptions,
    ILogger<StripePaymentService> logger) : IStripePaymentService
{
    private readonly StripeOptions _options = stripeOptions.Value;
    private readonly AppOptions _app = appOptions.Value;

    public async Task<string> CreateCheckoutSessionAsync(
        Guid tenantId,
        string tenantName,
        string tenantSlug,
        string userEmail,
        PlanTier tier,
        CancellationToken cancellationToken = default)
    {
        if (tier == PlanTier.Free)
        {
            throw new InvalidOperationException(ApiMessages.FreeTierCannotCheckout);
        }

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        // Fallback for local development if Stripe API keys have not yet been configured
        if (!_options.IsConfigured)
        {
            logger.LogWarning("Stripe is running in Sandbox / Mock mode for tenant {TenantId}. To enable real payments, configure Stripe:SecretKey and Stripe:WebhookSecret.", tenantId);

            // Directly simulate fulfillment in mock mode for instant developer convenience
            if (plan == null)
            {
                plan = TenantPlan.CreateDefault(tenantId, tier);
                plan.StripeCustomerId = "cus_mock_" + Guid.NewGuid().ToString("N")[..12];
                plan.StripeSubscriptionId = "sub_mock_" + Guid.NewGuid().ToString("N")[..12];
                plan.SubscriptionStatus = "active";
                plan.CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1);
                context.TenantPlans.Add(plan);
            }
            else
            {
                var template = TenantPlan.CreateDefault(tenantId, tier);
                plan.PlanTier = tier;
                plan.MaxDocuments = template.MaxDocuments;
                plan.MaxEmployees = template.MaxEmployees;
                plan.MaxStorageBytes = template.MaxStorageBytes;
                plan.StripeCustomerId ??= "cus_mock_" + Guid.NewGuid().ToString("N")[..12];
                plan.StripeSubscriptionId ??= "sub_mock_" + Guid.NewGuid().ToString("N")[..12];
                plan.SubscriptionStatus = "active";
                plan.CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1);
                plan.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);

            var mockSessionId = "simulated_sess_" + Guid.NewGuid().ToString("N");
            var mockSuccessUrl = $"{_app.BaseUrl}/app/workspaces/{tenantSlug}/employer/settings/subscription?status=success&session_id={mockSessionId}&tier={tier}";

            return mockSuccessUrl;
        }

        StripeConfiguration.ApiKey = _options.SecretKey;

        var priceId = tier switch
        {
            PlanTier.Pro => _options.ProPriceId,
            PlanTier.Enterprise => _options.EnterprisePriceId,
            _ => string.Empty
        };

        var lineItems = new List<SessionLineItemOptions>();

        if (!string.IsNullOrWhiteSpace(priceId))
        {
            lineItems.Add(new SessionLineItemOptions
            {
                Price = priceId,
                Quantity = 1
            });
        }
        else
        {
            // Dynamically defined price so checkout works immediately out of the box without manual dashboard setup
            var (unitAmount, name, description) = tier switch
            {
                PlanTier.Pro => (4900L, "Normora Pro Workspace", "100 Documents, 50 Members, 5 GB Storage, Department & Group Isolation"),
                PlanTier.Enterprise => (24900L, "Normora Enterprise Workspace", "10,000 Documents, 5,000 Members, 500 GB Storage, Compliance Audit Logs"),
                _ => (0L, "Free", "Free Plan")
            };

            lineItems.Add(new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = unitAmount,
                    Recurring = new SessionLineItemPriceDataRecurringOptions
                    {
                        Interval = "month"
                    },
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = name,
                        Description = description
                    }
                }
            });
        }

        var successUrl = $"{_app.BaseUrl}/app/workspaces/{tenantSlug}/employer/settings/subscription?status=success&session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{_app.BaseUrl}/app/workspaces/{tenantSlug}/employer/settings/subscription?status=cancelled";

        var sessionOptions = new Stripe.Checkout.SessionCreateOptions
        {
            Mode = "subscription",
            PaymentMethodTypes = ["card"],
            LineItems = lineItems,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = tenantId.ToString(),
                ["plan_tier"] = tier.ToString(),
                ["tenant_name"] = tenantName,
                ["tenant_slug"] = tenantSlug
            },
            SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString(),
                    ["plan_tier"] = tier.ToString()
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(plan?.StripeCustomerId))
        {
            sessionOptions.Customer = plan.StripeCustomerId;
        }
        else if (!string.IsNullOrWhiteSpace(userEmail))
        {
            sessionOptions.CustomerEmail = userEmail;
        }

        var sessionService = new Stripe.Checkout.SessionService();
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        return session.Url;
    }

    public async Task<string> CreateCustomerPortalSessionAsync(
        Guid tenantId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        if (!_options.IsConfigured)
        {
            logger.LogWarning("Stripe is running in mock mode. Returning local subscription URL for customer portal.");
            return string.IsNullOrWhiteSpace(returnUrl)
                ? $"{_app.BaseUrl}/settings/subscription?portal=simulated"
                : returnUrl;
        }

        if (string.IsNullOrWhiteSpace(plan?.StripeCustomerId))
        {
            throw new InvalidOperationException(ApiMessages.StripeBillingAccountNotFound);
        }

        StripeConfiguration.ApiKey = _options.SecretKey;

        var portalOptions = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = plan.StripeCustomerId,
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? $"{_app.BaseUrl}/settings/subscription"
                : returnUrl
        };

        var portalService = new Stripe.BillingPortal.SessionService();
        var portalSession = await portalService.CreateAsync(portalOptions, cancellationToken: cancellationToken);

        return portalSession.Url;
    }

    public async Task<bool> HandleWebhookAsync(
        string jsonPayload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            logger.LogWarning("Stripe webhook received with empty payload.");
            return false;
        }

        Event stripeEvent;
        try
        {
            if (_options.IsConfigured && !string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                stripeEvent = EventUtility.ConstructEvent(
                    jsonPayload,
                    stripeSignatureHeader,
                    _options.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            else
            {
                // In unconfigured or development mode, parse directly without signature validation
                stripeEvent = EventUtility.ParseEvent(jsonPayload);
            }
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe signature validation failed: {Message}", ex.Message);
            return false;
        }

        logger.LogInformation("Processing Stripe webhook event: {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                if (stripeEvent.Data.Object is Stripe.Checkout.Session checkoutSession)
                {
                    await HandleCheckoutSessionCompletedAsync(checkoutSession, cancellationToken);
                }
                break;

            case EventTypes.CustomerSubscriptionUpdated:
                if (stripeEvent.Data.Object is Stripe.Subscription updatedSub)
                {
                    await HandleSubscriptionUpdatedAsync(updatedSub, cancellationToken);
                }
                break;

            case EventTypes.CustomerSubscriptionDeleted:
                if (stripeEvent.Data.Object is Stripe.Subscription deletedSub)
                {
                    await HandleSubscriptionDeletedAsync(deletedSub, cancellationToken);
                }
                break;

            case EventTypes.InvoicePaymentFailed:
                if (stripeEvent.Data.Object is Stripe.Invoice failedInvoice)
                {
                    await HandleInvoicePaymentFailedAsync(failedInvoice, cancellationToken);
                }
                break;

            default:
                logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        return true;
    }

    public async Task<bool> FulfillSessionAsync(
        string sessionId,
        Guid expectedTenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        if (!_options.IsConfigured)
        {
            return true;
        }

        try
        {
            StripeConfiguration.ApiKey = _options.SecretKey;
            var sessionService = new Stripe.Checkout.SessionService();
            var session = await sessionService.GetAsync(sessionId, cancellationToken: cancellationToken);

            if (session == null)
            {
                logger.LogWarning("Checkout session {SessionId} not found in Stripe.", sessionId);
                return false;
            }

            // Verify tenant matches to prevent BOLA / misuse of other tenants' sessions
            if (session.Metadata == null ||
                !session.Metadata.TryGetValue("tenant_id", out var tenantIdStr) ||
                !Guid.TryParse(tenantIdStr, out var tenantId) ||
                tenantId != expectedTenantId)
            {
                logger.LogWarning("Session {SessionId} metadata tenant mismatch. Expected: {ExpectedTenantId}, Got: {MetadataTenantId}",
                    sessionId, expectedTenantId, session.Metadata?.GetValueOrDefault("tenant_id"));
                return false;
            }

            // Verify session is paid or complete
            if (session.PaymentStatus != "paid" && session.Status != "complete")
            {
                logger.LogInformation("Checkout session {SessionId} has status {Status} and payment status {PaymentStatus}. Not fulfilling yet.",
                    sessionId, session.Status, session.PaymentStatus);
                return false;
            }

            await HandleCheckoutSessionCompletedAsync(session, cancellationToken);
            return true;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error during synchronous fulfillment of session {SessionId}: {Message}", sessionId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fulfilling session {SessionId}: {Message}", sessionId, ex.Message);
            return false;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Stripe.Checkout.Session session, CancellationToken cancellationToken)
    {
        if (!session.Metadata.TryGetValue("tenant_id", out var tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            logger.LogWarning("Checkout session {SessionId} completed without a valid tenant_id in metadata.", session.Id);
            return;
        }

        session.Metadata.TryGetValue("plan_tier", out var tierStr);
        if (!Enum.TryParse<PlanTier>(tierStr, true, out var tier))
        {
            tier = PlanTier.Pro;
        }

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var template = TenantPlan.CreateDefault(tenantId, tier);

        if (plan == null)
        {
            plan = template;
            plan.StripeCustomerId = session.CustomerId;
            plan.StripeSubscriptionId = session.SubscriptionId;
            plan.SubscriptionStatus = "active";
            context.TenantPlans.Add(plan);
        }
        else
        {
            // Idempotency check: if already upgraded to this tier with this subscription, avoid duplicate processing
            if (plan.PlanTier == tier &&
                !string.IsNullOrWhiteSpace(plan.StripeSubscriptionId) &&
                plan.StripeSubscriptionId == session.SubscriptionId &&
                plan.SubscriptionStatus == "active")
            {
                logger.LogInformation("Tenant {TenantId} already fulfilled with tier {Tier} and subscription {SubscriptionId}.", tenantId, tier, plan.StripeSubscriptionId);
                return;
            }

            plan.PlanTier = tier;
            plan.MaxDocuments = template.MaxDocuments;
            plan.MaxEmployees = template.MaxEmployees;
            plan.MaxStorageBytes = template.MaxStorageBytes;
            plan.StripeCustomerId = session.CustomerId ?? plan.StripeCustomerId;
            plan.StripeSubscriptionId = session.SubscriptionId ?? plan.StripeSubscriptionId;
            plan.SubscriptionStatus = "active";
            plan.UpdatedAt = DateTime.UtcNow;
        }

        // Fetch subscription period details from Stripe if available
        if (!string.IsNullOrWhiteSpace(plan.StripeSubscriptionId) && _options.IsConfigured)
        {
            try
            {
                var subscriptionService = new Stripe.SubscriptionService();
                var sub = await subscriptionService.GetAsync(plan.StripeSubscriptionId, cancellationToken: cancellationToken);
                if (sub != null)
                {
                    plan.CurrentPeriodEndUtc = sub.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd ?? sub.CancelAt;
                    plan.CancelAtPeriodEnd = sub.CancelAtPeriodEnd;
                }
            }
            catch (Exception subEx)
            {
                logger.LogWarning(subEx, "Failed to retrieve subscription details for {SubscriptionId}", plan.StripeSubscriptionId);
            }
        }

        // Add audit log entry
        context.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            Action = "Subscription.CheckoutCompleted",
            TargetType = "TenantPlan",
            TargetId = plan.Id.ToString(),
            ActorUserId = "stripe_checkout",
            ActorEmail = "stripe@normora.internal",
            ActorName = "Stripe Checkout",
            Metadata = JsonSerializer.Serialize(new
            {
                tier = tier.ToString(),
                sessionId = session.Id,
                customerId = session.CustomerId,
                subscriptionId = session.SubscriptionId
            })
        });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Tenant {TenantId} successfully upgraded to {Tier} via Stripe checkout session {SessionId}", tenantId, tier, session.Id);
    }

    private async Task HandleSubscriptionUpdatedAsync(Stripe.Subscription subscription, CancellationToken cancellationToken)
    {
        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.StripeSubscriptionId == subscription.Id, cancellationToken);

        if (plan == null && subscription.Metadata.TryGetValue("tenant_id", out var tenantIdStr) && Guid.TryParse(tenantIdStr, out var tenantId))
        {
            plan = await context.TenantPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        }

        if (plan == null)
        {
            logger.LogWarning("No TenantPlan found matching Stripe subscription {SubscriptionId}", subscription.Id);
            return;
        }

        plan.SubscriptionStatus = subscription.Status;
        plan.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;
        plan.CurrentPeriodEndUtc = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd ?? subscription.CancelAt;
        plan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Subscription {SubscriptionId} updated. Status: {Status}, CancelAtPeriodEnd: {CancelAtPeriodEnd}", subscription.Id, subscription.Status, subscription.CancelAtPeriodEnd);
    }

    private async Task HandleSubscriptionDeletedAsync(Stripe.Subscription subscription, CancellationToken cancellationToken)
    {
        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.StripeSubscriptionId == subscription.Id, cancellationToken);

        if (plan == null) return;

        var freeTemplate = TenantPlan.CreateDefault(plan.TenantId, PlanTier.Free);
        plan.PlanTier = PlanTier.Free;
        plan.MaxDocuments = freeTemplate.MaxDocuments;
        plan.MaxEmployees = freeTemplate.MaxEmployees;
        plan.MaxStorageBytes = freeTemplate.MaxStorageBytes;
        plan.SubscriptionStatus = "canceled";
        plan.CancelAtPeriodEnd = false;
        plan.UpdatedAt = DateTime.UtcNow;

        context.AuditLogs.Add(new AuditLog
        {
            TenantId = plan.TenantId,
            Action = "Subscription.Cancelled",
            TargetType = "TenantPlan",
            TargetId = plan.Id.ToString(),
            ActorUserId = "stripe_webhook",
            ActorEmail = "stripe@normora.internal",
            ActorName = "Stripe Webhook",
            Metadata = JsonSerializer.Serialize(new
            {
                subscriptionId = subscription.Id,
                status = "canceled"
            })
        });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Tenant {TenantId} subscription was deleted/cancelled. Reverted to Free tier.", plan.TenantId);
    }

    private async Task HandleInvoicePaymentFailedAsync(Stripe.Invoice invoice, CancellationToken cancellationToken)
    {
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId ?? invoice.Lines?.Data?.FirstOrDefault()?.SubscriptionId;
        if (string.IsNullOrWhiteSpace(subscriptionId)) return;

        var plan = await context.TenantPlans
            .FirstOrDefaultAsync(p => p.StripeSubscriptionId == subscriptionId, cancellationToken);

        if (plan == null) return;

        plan.SubscriptionStatus = "past_due";
        plan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Invoice payment failed for tenant {TenantId}, subscription {SubscriptionId}. Status set to past_due.", plan.TenantId, subscriptionId);
    }
}
