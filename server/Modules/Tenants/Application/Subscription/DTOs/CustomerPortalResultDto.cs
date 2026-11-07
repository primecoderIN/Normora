namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

/// <summary>
/// Encapsulates the self-service billing management portal URL for an organization.
/// </summary>
public record CustomerPortalResultDto(
    // Fully-qualified redirect URL pointing to the customer billing management portal.
    string PortalUrl
);

