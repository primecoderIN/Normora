using MediatR;

namespace Normora.Modules.Tenants.Application.Users.Commands;

/// <summary>
/// Command to update a user's department and user group assignments within the current tenant.
/// </summary>
/// <param name="UserId">The ID of the user whose assignments are being updated.</param>
/// <param name="DepartmentIds">The new list of directly assigned department IDs.</param>
/// <param name="UserGroupIds">The new list of user group IDs the user should belong to.</param>
public record UpdateUserAssignmentsCommand(Guid UserId, List<Guid> DepartmentIds, List<Guid> UserGroupIds) : IRequest<bool>;

