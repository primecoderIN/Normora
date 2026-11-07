using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Command to provision a new organizational department within the ambient tenant.
/// Handled by <see cref="CreateDepartmentCommandHandler"/>.
/// </summary>
/// <remarks>
/// Authorization & Validation:
/// - Caller must be an authenticated employer or administrator within the ambient tenant context.
/// - Department names must be non-empty and unique per tenant (enforced by FluentValidator and DB unique index).
/// - Implements <see cref="IAuditableRequest"/> to record the creation event in the audit trail.
/// </remarks>
/// <param name="Name">The display name of the department (e.g. "Engineering", "People Operations").</param>
/// <param name="Description">Optional functional description explaining the scope of this departmental division.</param>
public record CreateDepartmentCommand(string Name, string? Description = null) : IRequest<Guid>, IAuditableRequest
{
    public string Action => "Department.Created";
    public string TargetType => "Department";
    public string? GetTargetId(object? response) => response is Guid id ? id.ToString() : null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name, description = Description });
}

