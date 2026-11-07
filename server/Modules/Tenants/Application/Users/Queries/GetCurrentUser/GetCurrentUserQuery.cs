using MediatR;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Query to retrieve the currently authenticated user's profile, tenant memberships, and pending invitations.
/// Handled by <see cref="GetCurrentUserQueryHandler"/>.
/// </summary>
/// <remarks>
/// Workspace Resolution:
/// - Uses the authenticated OIDC 'sub' (ExternalUserId) from ClaimsPrincipal.
/// - Performs just-in-time (JIT) provisioning of the User entity if this is their first login.
/// - Returns all associated workspace memberships with role designations to populate the client workspace switcher.
/// </remarks>
public record GetCurrentUserQuery : IRequest<CurrentUserDto>;

/// <summary>
/// Data transfer object representing the authenticated user's comprehensive profile.
/// </summary>
public record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAt,
    List<UserTenantMembershipDto> Memberships,
    List<PendingInvitationDto> PendingInvitations
);

/// <summary>
/// Represents a pending invitation to join a tenant extended to this user's email address.
/// </summary>
public record PendingInvitationDto(Guid Token, string TenantName);

/// <summary>
/// Represents a single tenant membership entry for a user, including their role within that tenant.
/// </summary>
public record UserTenantMembershipDto(
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    string Role,
    bool IsPersonal
);
