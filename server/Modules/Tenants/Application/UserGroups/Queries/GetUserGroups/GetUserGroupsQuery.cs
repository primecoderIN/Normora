using MediatR;
using Normora.Modules.Tenants.Application.UserGroups.DTOs;

namespace Normora.Modules.Tenants.Application.UserGroups.Queries;

/// <summary>
/// Query to retrieve a list of all user groups for the current tenant.
/// </summary>
public record GetUserGroupsQuery : IRequest<List<UserGroupDto>>;

