using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to create a new user security group within the active tenant and associate it with initial departments.
/// Handled by <see cref="CreateUserGroupCommandHandler"/>.
/// </summary>
/// <remarks>
/// Access Control & Boundaries:
/// - Verifies user group name is non-empty and unique within the ambient TenantId.
/// - Validates that all supplied DepartmentIds belong to the caller's tenant (BOLA prevention).
/// - Creates junction entries in <see cref="Domain.UserGroupDepartment"/> for each department.
/// - Implements <see cref="IAuditableRequest"/> to record security group creation in the audit log.
/// </remarks>
/// <param name="Name">The display name of the user group (e.g. "North America Sales").</param>
/// <param name="Description">Optional functional description explaining the group's purpose.</param>
/// <param name="DepartmentIds">Initial set of department scopes granted to all group members.</param>
public record CreateUserGroupCommand(string Name, string? Description, List<Guid> DepartmentIds) : IRequest<Guid>, IAuditableRequest
{
    public string Action => "UserGroup.Created";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => response is Guid id ? id.ToString() : null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name });
}

