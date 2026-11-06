namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

/// <summary>Creates a return URL for the active tenant's payment-provider billing portal.</summary>
public record CustomerPortalResultDto(string PortalUrl);

