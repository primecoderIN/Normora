using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Contracts.Tenants;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Resolves the user's formatted display name (or email fallback) using their Keycloak subject identifier.
/// </summary>
public class GetUserDisplayNameQueryHandler(TenantsDbContext context)
    : IRequestHandler<GetUserDisplayNameQuery, string?>
{
    public async Task<string?> Handle(GetUserDisplayNameQuery request, CancellationToken cancellationToken)
    {
        // Query user metadata projected to display name or email fallback
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.ExternalUserId == request.ExternalUserId)
            .Select(u => new { u.DisplayName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(user?.DisplayName)
            ? user.DisplayName
            : user?.Email;
    }
}

