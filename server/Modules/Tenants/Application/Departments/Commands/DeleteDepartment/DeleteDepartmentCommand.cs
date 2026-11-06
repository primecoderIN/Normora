using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Command to delete a specific department from the current tenant.
/// </summary>
public record DeleteDepartmentCommand(Guid Id) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Department.Deleted";
    public string TargetType => "Department";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => null;
}

