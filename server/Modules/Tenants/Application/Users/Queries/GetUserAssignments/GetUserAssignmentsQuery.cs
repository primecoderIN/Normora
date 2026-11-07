using MediatR;
using Normora.Modules.Tenants.Application.Users.DTOs;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Query to retrieve the direct department and user group assignments for a specific user within the current tenant.
/// Handled by <see cref="GetUserAssignmentsQueryHandler"/>.
/// </summary>
/// <remarks>
/// Authorization & Isolation:
/// - Verifies that the requested user holds an active membership in the caller's ambient TenantId (BOLA protection).
/// - Returns distinct lists of DepartmentIds and UserGroupIds to populate management modal checkboxes.
/// </remarks>
/// <param name="UserId">The ID of the user whose assignments are to be retrieved.</param>
public record GetUserAssignmentsQuery(Guid UserId) : IRequest<UserAssignmentsDto>;

