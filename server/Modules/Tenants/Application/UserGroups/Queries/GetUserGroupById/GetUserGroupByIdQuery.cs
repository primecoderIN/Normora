using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.DTOs;

namespace Normora.Modules.Tenants.Application.UserGroups.Queries;

/// <summary>
/// Query to retrieve detailed configuration and membership roster of a user group by its ID.
/// Handled by <see cref="GetUserGroupByIdQueryHandler"/>.
/// </summary>
/// <remarks>
/// Authorization & Isolation:
/// - Verifies that the requested user group belongs to the caller's ambient TenantId.
/// - Throws BolaException (404) if the group is absent or belongs to another organization.
/// </remarks>
/// <param name="Id">The unique identifier of the user group.</param>
public record GetUserGroupByIdQuery(Guid Id) : IRequest<UserGroupDetailDto>;

