using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Tenants.Commands;

/// <summary>
/// Command to administratively suspend a tenant workspace, instantly locking out all member access.
/// Handled by <see cref="SuspendTenantCommandHandler"/>.
/// </summary>
/// <remarks>
/// Security & Access Lock:
/// - Restricted to platform super-administrators or triggered on non-payment / terms-of-service violations.
/// - Sets <see cref="Domain.Tenant.Status"/> to Suspended, triggering immediate 403 Forbidden interceptors
///   across all subsequent API calls from any membership in that tenant.
/// - Implements <see cref="IAuditableRequest"/> to record executive suspension in the platform audit log.
/// </remarks>
/// <param name="TenantId">The unique identifier of the tenant to suspend.</param>
public record SuspendTenantCommand(Guid TenantId) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Tenant.Suspended";
    public string TargetType => "Tenant";
    public string? GetTargetId(object? response) => TenantId.ToString();
    public string? GetMetadata(object? response) => null;
}

