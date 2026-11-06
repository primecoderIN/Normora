using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Command to create a new department within the current tenant.
/// </summary>
/// <param name="Name">The name of the new department.</param>
public record CreateDepartmentCommand(string Name, string? Description = null) : IRequest<Guid>, IAuditableRequest
{
    public string Action => "Department.Created";
    public string TargetType => "Department";
    public string? GetTargetId(object? response) => response is Guid id ? id.ToString() : null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name, description = Description });
}

