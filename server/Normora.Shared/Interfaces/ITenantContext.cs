using System;

namespace Normora.Shared.Interfaces;

/// <summary>
/// A scoped service that holds the context of the current tenant for the duration of the HTTP request.
/// This allows deeply nested services (like EF Core DbContexts) to transparently access the active TenantId.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The unique identifier of the active tenant.
    /// Used heavily by Entity Framework Global Query Filters to enforce strict data isolation.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// The role the current user holds within this specific tenant (e.g., 'Employer', 'Employee').
    /// </summary>
    string? TenantRole { get; }

    /// <summary>
    /// The unique list of departments the user has access to in this tenant.
    /// Derived from direct department assignments and group inheritance.
    /// An empty list implies the user can only access "Company Wide" documents.
    /// </summary>
    IReadOnlyCollection<Guid> EffectiveDepartments { get; }

    /// <summary>
    /// True if a tenant context was successfully extracted and validated during this request.
    /// </summary>
    bool IsTenantResolved { get; }
    
    /// <summary>
    /// Manually injects the tenant context.
    /// This is typically called by the TenantResolutionMiddleware after validating the X-Tenant-Id header.
    /// </summary>
    /// <param name="tenantId">The resolved tenant ID.</param>
    /// <param name="role">The user's role in this tenant.</param>
    /// <param name="effectiveDepartments">The resolved effective departments for the user.</param>
    void SetContext(Guid tenantId, string role, IReadOnlyCollection<Guid> effectiveDepartments);
}
