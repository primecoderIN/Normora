namespace Normora.Modules.Tenants.Application.Users.DTOs;

/// <summary>
/// Metric counts for tenant dashboard (pending invitations and active admin seats).
/// </summary>
public record TenantStatsDto(
    // Count of invitations in "Pending" status awaiting employee acceptance.
    int PendingInvites,

    // Count of active tenant memberships holding "Owner" or "Admin" roles.
    int AdminSeats
);

