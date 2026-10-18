using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Contracts.Tenants;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Resolves an internal application User Guid from an external Keycloak subject ID.
/// </summary>
public class GetUserIdQueryHandler(TenantsDbContext context) : IRequestHandler<GetUserIdQuery, Guid?>
{
    public async Task<Guid?> Handle(GetUserIdQuery request, CancellationToken cancellationToken)
    {
        // Query internal database Guid mapped to Keycloak subject ID
        return await context.Users
            .AsNoTracking()
            .Where(u => u.KeycloakUserId == request.KeycloakUserId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

