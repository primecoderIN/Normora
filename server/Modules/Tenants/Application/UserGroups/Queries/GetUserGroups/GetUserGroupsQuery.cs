using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.DTOs;

namespace Normora.Modules.Tenants.Application.UserGroups.Queries;

/// <summary>
/// Query to retrieve all user security groups configured within the caller's tenant organization.
/// Handled by <see cref="GetUserGroupsQueryHandler"/>.
/// </summary>
/// <remarks>
/// Scoping:
/// - Filtered strictly by ambient TenantId via ITenantContext.
/// - Projects counts of active members and departments for clean summary table rendering.
/// </remarks>
public record GetUserGroupsQuery : IRequest<List<UserGroupDto>>;

