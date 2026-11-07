using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Command to delete a specific department from the active tenant organization.
/// Handled by <see cref="DeleteDepartmentCommandHandler"/>.
/// </summary>
/// <remarks>
/// Security & Referential Integrity:
/// - Verifies that the targeted department belongs strictly to the caller's ambient TenantId (BOLA protection).
/// - Restricts deletion if active employee memberships or user groups remain explicitly bound (DeleteBehavior.Restrict).
/// - Implements <see cref="IAuditableRequest"/> to trace administrator deletion actions.
/// </remarks>
/// <param name="Id">The unique identifier of the department to delete.</param>
public record DeleteDepartmentCommand(Guid Id) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Department.Deleted";
    public string TargetType => "Department";
    public string? GetTargetId(object? response) => Id.ToString();
    public string? GetMetadata(object? response) => null;
}

