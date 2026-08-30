using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

public record GetInvitationQuery(Guid Token) : IRequest<InvitationDto?>;

public record InvitationDto(Guid Token, string Email, string TenantName, string TenantSlug, string Status, DateTime ExpiresAt);
