using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Users;

public record TenantStatsDto(int PendingInvites, int AdminSeats);

public record GetTenantStatsQuery(Guid TenantId) : IRequest<TenantStatsDto>;

public sealed class GetTenantStatsQueryHandler(TenantsDbContext dbContext) : IRequestHandler<GetTenantStatsQuery, TenantStatsDto>
{
    public async Task<TenantStatsDto> Handle(GetTenantStatsQuery request, CancellationToken cancellationToken)
    {
        var pendingInvites = await dbContext.TenantInvitations
            .Where(i => i.TenantId == request.TenantId && i.Status == InvitationStatus.Pending)
            .CountAsync(cancellationToken);

        var adminSeats = await dbContext.TenantMemberships
            .Where(m => m.TenantId == request.TenantId && m.Role == TenantRole.Admin)
            .CountAsync(cancellationToken);

        return new TenantStatsDto(pendingInvites, adminSeats);
    }
}
