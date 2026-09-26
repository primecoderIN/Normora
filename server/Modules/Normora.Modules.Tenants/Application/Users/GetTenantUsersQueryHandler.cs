using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Users;

public class GetTenantUsersQueryHandler(TenantsDbContext context) : IRequestHandler<GetTenantUsersQuery, List<TenantUserDto>>
{
    public async Task<List<TenantUserDto>> Handle(GetTenantUsersQuery request, CancellationToken cancellationToken)
    {
        var members = await context.TenantMemberships
            .Include(m => m.User)
            .Where(m => m.TenantId == request.TenantId)
            .Select(m => new TenantUserDto(
                m.UserId,
                m.User.Email ?? string.Empty,
                m.User.DisplayName ?? string.Empty,
                m.Role.ToString(),
                m.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return members;
    }
}
