using MediatR;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to update the member users and department assignments of an existing user group.
/// </summary>
/// <param name="Id">The unique identifier of the user group.</param>
/// <param name="MemberUserIds">The list of user IDs to assign as members of this group.</param>
/// <param name="DepartmentIds">The list of department IDs to grant access to this group.</param>
public record UpdateUserGroupAssignmentsCommand(Guid Id, List<Guid> MemberUserIds, List<Guid> DepartmentIds) : IRequest<bool>;

