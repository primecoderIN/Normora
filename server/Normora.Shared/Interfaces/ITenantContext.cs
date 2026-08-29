using System;

namespace Normora.Shared.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantRole { get; }
    bool IsTenantResolved { get; }
    
    void SetContext(Guid tenantId, string role);
}
