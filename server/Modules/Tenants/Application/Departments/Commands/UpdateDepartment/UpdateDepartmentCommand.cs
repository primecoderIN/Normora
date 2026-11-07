using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Command to modify the name or description of an existing organizational department.
/// Handled by <see cref="UpdateDepartmentCommandHandler"/>.
/// </summary>
/// <remarks>
/// Validation & Uniqueness:
/// - Verifies department ownership within ambient TenantId (throws BolaException if unmapped).
/// - Enforces name uniqueness against sibling departments within the tenant.
/// - Implements <see cref="IAuditableRequest"/> to audit changes.
/// </remarks>
/// <param name="Id">The unique identifier of the department.</param>
/// <param name="Name">The updated name for the department.</param>
/// <param name="Description">The updated description for the department.</param>
public record UpdateDepartmentCommand(Guid Id, string Name, string? Description = null) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Department.Updated";
    public string TargetType => "Department";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name, description = Description });
}

