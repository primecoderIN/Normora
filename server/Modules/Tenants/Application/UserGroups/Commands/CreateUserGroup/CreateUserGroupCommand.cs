using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to create a new user group within the current tenant and assign it to departments.
/// </summary>
/// <param name="Name">The name of the user group.</param>
public record CreateUserGroupCommand(string Name, string? Description, List<Guid> DepartmentIds) : IRequest<Guid>, IAuditableRequest
{
    public string Action => "UserGroup.Created";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => response is Guid id ? id.ToString() : null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name });
}

