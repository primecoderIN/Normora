namespace Normora.Modules.Tenants.Application.Invitations.DTOs;

/// <summary>
/// Data transfer object representing the public preview of a tenant workspace invitation.
/// Rendered on the client onboarding view (`/accept-invite?token=...`) prior to confirmation.
/// </summary>
public record InvitationDto(
    // The unique cryptographic verification token.
    Guid Token,

    // The intended recipient's email address.
    string Email,

    // The organization name extending the invitation (e.g. "Acme Corp").
    string TenantName,

    // The vanity URL slug of the inviting tenant.
    string TenantSlug,

    // Current state of the invitation ("Pending", "Accepted", "Expired", "Rejected").
    string Status,

    // UTC expiration timestamp after which the token is invalid (48 hours post-creation).
    DateTime ExpiresAt
);

