using MediatR;

namespace Normora.Modules.Tenants.Application.Users.Commands;

/// <summary>
/// Command to atomically update an employee's direct department memberships and security user groups.
/// Handled by <see cref="UpdateUserAssignmentsCommandHandler"/>.
/// </summary>
/// <remarks>
/// Permission Scoping Workflow:
/// - Verifies target user holds an active membership within the ambient TenantId (BOLA validation).
/// - Validates that all supplied DepartmentIds and UserGroupIds are owned by the current tenant.
/// - Synchronizes <see cref="Domain.MembershipDepartment"/> and <see cref="Domain.UserGroupMembership"/> junction records.
/// - Determines the exact set of document chunks accessible to the employee during conversational RAG queries.
/// </remarks>
/// <param name="UserId">The internal Guid of the user being configured.</param>
/// <param name="DepartmentIds">Direct departmental assignments.</param>
/// <param name="UserGroupIds">Assigned user security groups.</param>
public record UpdateUserAssignmentsCommand(Guid UserId, List<Guid> DepartmentIds, List<Guid> UserGroupIds) : IRequest<bool>;

