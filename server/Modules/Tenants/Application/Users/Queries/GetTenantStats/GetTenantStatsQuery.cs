using MediatR;
using Normora.Modules.Tenants.Application.Users.DTOs;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Query to count pending invites and active administrators for a tenant organization.
/// Handled by <see cref="GetTenantStatsQueryHandler"/>.
/// </summary>
/// <remarks>
/// Authorization & Isolation:
/// - Verifies that the requested TenantId matches the caller's ambient tenant context (BOLA protection).
/// - Restricts execution to Owner/Admin roles.
/// </remarks>
/// <param name="TenantId">The unique identifier of the tenant.</param>
public record GetTenantStatsQuery(Guid TenantId) : IRequest<TenantStatsDto>;

