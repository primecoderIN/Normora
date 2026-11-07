using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users.Commands;

/// <summary>
/// Command to soft-remove an employee membership from the active tenant organization.
/// Handled by <see cref="RemoveEmployeeCommandHandler"/>.
/// </summary>
/// <remarks>
/// Safety Invariants & Last-Admin Protection:
/// - Verifies target membership belongs strictly to the caller's ambient TenantId (BOLA protection).
/// - Enforces the "Last Administrator" rule: will reject removal if target is the sole active Owner/Admin.
/// - Sets `RemovedAt = DateTime.UtcNow` and clears assigned user groups and department links.
/// - Preserves historic messages, citations, and audit logs associated with the user.
/// - Implements <see cref="IAuditableRequest"/> to trace user offboarding.
/// </remarks>
/// <param name="MembershipId">The unique identifier of the <see cref="Domain.TenantMembership"/> record.</param>
public record RemoveEmployeeCommand(Guid MembershipId) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Employee.Removed";
    public string TargetType => "Employee";
    public string? GetTargetId(object? response) => MembershipId.ToString();
    public string? GetMetadata(object? response) => null;
}

