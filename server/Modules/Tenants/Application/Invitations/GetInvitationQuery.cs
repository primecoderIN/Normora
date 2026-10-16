using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Query to retrieve the details of a pending tenant invitation by its token.
/// </summary>
/// <param name="Token">The unique token identifying the invitation.</param>
public record GetInvitationQuery(Guid Token) : IRequest<InvitationDto?>;

/// <summary>
/// Data transfer object representing the public details of a tenant invitation.
/// </summary>
public record InvitationDto(Guid Token, string Email, string TenantName, string TenantSlug, string Status, DateTime ExpiresAt);
