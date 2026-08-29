using System;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantRole { get; private set; }
    public bool IsTenantResolved => TenantId.HasValue;

    public void SetContext(Guid tenantId, string role)
    {
        if (IsTenantResolved)
        {
            throw new InvalidOperationException("Tenant context is already set for this request.");
        }

        TenantId = tenantId;
        TenantRole = role;
    }
}
