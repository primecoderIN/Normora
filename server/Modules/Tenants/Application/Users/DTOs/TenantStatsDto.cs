namespace Normora.Modules.Tenants.Application.Users.DTOs;

/// <summary>
/// Metric counts for tenant dashboard (pending invitations and active admin seats).
/// </summary>
public record TenantStatsDto(int PendingInvites, int AdminSeats);

