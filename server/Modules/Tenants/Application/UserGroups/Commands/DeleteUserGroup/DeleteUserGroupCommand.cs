using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Command to delete a specific user group from the current tenant.
/// </summary>
public record DeleteUserGroupCommand(Guid Id) : IRequest<bool>, IAuditableRequest
{
    public string Action => "UserGroup.Deleted";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => null;
}

