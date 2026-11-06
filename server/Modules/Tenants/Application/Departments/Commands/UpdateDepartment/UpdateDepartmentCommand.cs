using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Command to update the details of an existing department.
/// </summary>
/// <param name="Id">The unique identifier of the department.</param>
/// <param name="Name">The new name for the department.</param>
/// <param name="Description">The updated description for the department.</param>
public record UpdateDepartmentCommand(Guid Id, string Name, string? Description = null) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Department.Updated";
    public string TargetType => "Department";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name, description = Description });
}

