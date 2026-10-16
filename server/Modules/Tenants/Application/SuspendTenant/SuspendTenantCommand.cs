using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.SuspendTenant;

/// <summary>
/// Command to suspend a tenant, blocking all member access.
/// </summary>
/// <param name="TenantId">The unique identifier of the tenant to suspend.</param>
public record SuspendTenantCommand(Guid TenantId) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Tenant.Suspended";
    public string TargetType => "Tenant";
    public string? GetTargetId(object? response) => TenantId.ToString();
    public string? GetMetadata(object? response) => null;
}

