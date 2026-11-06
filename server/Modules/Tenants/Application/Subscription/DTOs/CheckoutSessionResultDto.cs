namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

/// <summary>Creates a hosted checkout session for an eligible tenant plan change.</summary>
public record CheckoutSessionResultDto(string ActionUrl, Dictionary<string, string> Params);

