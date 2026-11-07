using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to update the name, description, and department associations of an existing user security group.
/// Handled by <see cref="UpdateUserGroupCommandHandler"/>.
/// </summary>
/// <remarks>
/// Differential Synchronization:
/// - Verifies user group ownership within ambient TenantId (BOLA protection).
/// - Validates group name uniqueness within the tenant.
/// - Synchronizes the <see cref="Domain.UserGroupDepartment"/> junction collection to match supplied DepartmentIds.
/// - Implements <see cref="IAuditableRequest"/> to record changes in the audit trail.
/// </remarks>
/// <param name="Id">The unique identifier of the user group to update.</param>
/// <param name="Name">The updated display name for the user group.</param>
/// <param name="Description">The updated description for the user group.</param>
/// <param name="DepartmentIds">The new collection of department scopes mapped to this user group.</param>
public record UpdateUserGroupCommand(Guid Id, string Name, string? Description, List<Guid> DepartmentIds) : IRequest<bool>, IAuditableRequest
{
    public string Action => "UserGroup.Updated";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name });
}

