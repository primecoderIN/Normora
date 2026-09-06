using System;
using Normora.Shared.Interfaces;

namespace Normora.Api.Services;

/// <summary>
/// A scoped service that holds the state of the active tenant during an HTTP Request.
/// Populated by the TenantResolutionMiddleware early in the pipeline.
/// </summary>
public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantRole { get; private set; }
    public IReadOnlyCollection<Guid> EffectiveDepartments { get; private set; } = Array.Empty<Guid>();
    
    public bool IsTenantResolved => TenantId.HasValue;

    /// <summary>
    /// Locks in the tenant context for this request.
    /// To prevent spoofing or accidental overrides, this method throws if called more than once per request.
    /// </summary>
    public void SetContext(Guid tenantId, string role, IReadOnlyCollection<Guid> effectiveDepartments)
    {
        if (IsTenantResolved)
        {
            throw new InvalidOperationException("Tenant context is already set for this request.");
        }

        TenantId = tenantId;
        TenantRole = role;
        EffectiveDepartments = effectiveDepartments;
    }
}
