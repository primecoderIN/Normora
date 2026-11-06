using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to update the details and department assignments of an existing user group.
/// </summary>
/// <param name="Id">The unique identifier of the user group.</param>
/// <param name="Name">The new name for the user group.</param>
public record UpdateUserGroupCommand(Guid Id, string Name, string? Description, List<Guid> DepartmentIds) : IRequest<bool>, IAuditableRequest
{
    public string Action => "UserGroup.Updated";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name });
}

