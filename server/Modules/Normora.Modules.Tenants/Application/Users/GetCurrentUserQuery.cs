using MediatR;

namespace Normora.Modules.Tenants.Application.Users;

public record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public record CurrentUserDto(Guid Id, string Email, string DisplayName, List<UserTenantMembershipDto> Memberships);

public record UserTenantMembershipDto(Guid TenantId, string TenantName, string TenantSlug, string Role);
