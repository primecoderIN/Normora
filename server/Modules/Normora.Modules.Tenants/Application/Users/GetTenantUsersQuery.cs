using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users;

public record GetTenantUsersQuery(Guid TenantId) : IRequest<List<TenantUserDto>>;

public record TenantUserDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    DateTime JoinedAt
);
