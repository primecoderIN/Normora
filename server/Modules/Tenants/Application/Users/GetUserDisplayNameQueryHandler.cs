using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Contracts.Tenants;

namespace Normora.Modules.Tenants.Application.Users;

public class GetUserDisplayNameQueryHandler(TenantsDbContext context)
    : IRequestHandler<GetUserDisplayNameQuery, string?>
{
    public async Task<string?> Handle(GetUserDisplayNameQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.KeycloakUserId == request.KeycloakUserId)
            .Select(u => new { u.DisplayName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(user?.DisplayName)
            ? user.DisplayName
            : user?.Email;

    }
}
