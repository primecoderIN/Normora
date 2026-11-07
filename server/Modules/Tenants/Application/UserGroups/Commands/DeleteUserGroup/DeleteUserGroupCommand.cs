using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to delete a specific user security group from the current tenant organization.
/// Handled by <see cref="DeleteUserGroupCommandHandler"/>.
/// </summary>
/// <remarks>
/// Cascade Behavior:
/// - Verifies that the targeted group belongs strictly to the caller's TenantId (throws BolaException if unmapped).
/// - Automatically removes all user membership associations (<see cref="Domain.UserGroupMembership"/>).
/// - Cleans up all departmental junction mappings (<see cref="Domain.UserGroupDepartment"/>).
/// - Implements <see cref="IAuditableRequest"/> to trace administrator permission modifications.
/// </remarks>
/// <param name="Id">The unique identifier of the user group to remove.</param>
public record DeleteUserGroupCommand(Guid Id) : IRequest<bool>, IAuditableRequest
{
    public string Action => "UserGroup.Deleted";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => null;
}

