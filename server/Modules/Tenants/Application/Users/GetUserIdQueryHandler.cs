using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Contracts.Tenants;

namespace Normora.Modules.Tenants.Application.Users;

public class GetUserIdQueryHandler(TenantsDbContext context) : IRequestHandler<GetUserIdQuery, Guid?>
{
    public async Task<Guid?> Handle(GetUserIdQuery request, CancellationToken cancellationToken)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.KeycloakUserId == request.KeycloakUserId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
