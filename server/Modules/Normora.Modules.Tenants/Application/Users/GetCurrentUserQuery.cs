using MediatR;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Query to retrieve the currently authenticated user's profile and their tenant memberships.
/// </summary>
public record GetCurrentUserQuery : IRequest<CurrentUserDto>;

/// <summary>
/// Data transfer object representing the authenticated user's profile.
/// </summary>
public record CurrentUserDto(Guid Id, string Email, string DisplayName, List<UserTenantMembershipDto> Memberships, List<PendingInvitationDto> PendingInvitations);

/// <summary>
/// Represents a pending invitation to join a tenant.
/// </summary>
public record PendingInvitationDto(Guid Token, string TenantName);

/// <summary>
/// Represents a single tenant membership entry for a user, including their role within that tenant.
/// </summary>
public record UserTenantMembershipDto(Guid TenantId, string TenantName, string TenantSlug, string Role, bool IsPersonal);
