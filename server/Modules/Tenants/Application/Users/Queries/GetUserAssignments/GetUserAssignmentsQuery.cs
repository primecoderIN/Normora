using MediatR;
using Normora.Modules.Tenants.Application.Users.DTOs;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Query to retrieve the department and user group assignments for a specific user within the current tenant.
/// </summary>
/// <param name="UserId">The ID of the user whose assignments are to be retrieved.</param>
public record GetUserAssignmentsQuery(Guid UserId) : IRequest<UserAssignmentsDto>;

