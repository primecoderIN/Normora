using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Metric counts for tenant dashboard (pending invitations and active admin seats).
/// </summary>
public record TenantStatsDto(int PendingInvites, int AdminSeats);

/// <summary>
/// Query to count pending invites and active administrators for a tenant.
/// </summary>
public record GetTenantStatsQuery(Guid TenantId) : IRequest<TenantStatsDto>;

/// <summary>
/// Executes counts of pending invitations and admin memberships.
/// </summary>
public sealed class GetTenantStatsQueryHandler(TenantsDbContext dbContext) : IRequestHandler<GetTenantStatsQuery, TenantStatsDto>
{
    public async Task<TenantStatsDto> Handle(GetTenantStatsQuery request, CancellationToken cancellationToken)
    {
        // Count unaccepted invitations awaiting user confirmation
        var pendingInvites = await dbContext.TenantInvitations
            .Where(i => i.TenantId == request.TenantId && i.Status == InvitationStatus.Pending)
            .CountAsync(cancellationToken);

        // Count members with the Admin role
        var adminSeats = await dbContext.TenantMemberships
            .Where(m => m.TenantId == request.TenantId && m.Role == TenantRole.Admin)
            .CountAsync(cancellationToken);

        return new TenantStatsDto(pendingInvites, adminSeats);
    }
}

