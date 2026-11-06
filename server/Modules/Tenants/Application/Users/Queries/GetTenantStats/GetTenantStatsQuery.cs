using MediatR;
using Normora.Modules.Tenants.Application.Users.DTOs;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Query to count pending invites and active administrators for a tenant.
/// </summary>
public record GetTenantStatsQuery(Guid TenantId) : IRequest<TenantStatsDto>;

