namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

/// <summary>
/// Encapsulates hosted checkout session parameters dispatched to the Angular client.
/// Used by the client to render and auto-submit an HTML form targeting the PayU gateway.
/// </summary>
public record CheckoutSessionResultDto(
    // The target POST URL of the payment gateway hosted page.
    string ActionUrl,

    // Dictionary of pre-signed parameters required by the gateway.
    Dictionary<string, string> Params
);

