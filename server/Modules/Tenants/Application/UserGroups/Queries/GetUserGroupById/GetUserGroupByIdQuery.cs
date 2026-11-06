using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.DTOs;

namespace Normora.Modules.Tenants.Application.UserGroups.Queries;

/// <summary>
/// Query to retrieve a specific user group by its ID within the current tenant.
/// </summary>
public record GetUserGroupByIdQuery(Guid Id) : IRequest<UserGroupDetailDto>;

