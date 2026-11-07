using MediatR;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to atomically synchronize the member employees and department scopes of an existing user group.
/// Handled by <see cref="UpdateUserGroupAssignmentsCommandHandler"/>.
/// </summary>
/// <remarks>
/// Atomic Multi-Junction Synchronization:
/// - Reconciles <see cref="Domain.UserGroupMembership"/> junction records against MemberUserIds.
/// - Reconciles <see cref="Domain.UserGroupDepartment"/> junction records against DepartmentIds.
/// - Validates that all referenced users and departments strictly belong to the ambient tenant.
/// </remarks>
/// <param name="Id">The unique identifier of the target user group.</param>
/// <param name="MemberUserIds">The complete set of employee UserIds to assign to this group.</param>
/// <param name="DepartmentIds">The complete set of department IDs to grant access to this group.</param>
public record UpdateUserGroupAssignmentsCommand(Guid Id, List<Guid> MemberUserIds, List<Guid> DepartmentIds) : IRequest<bool>;

