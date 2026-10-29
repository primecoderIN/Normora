namespace Normora.Shared.Interfaces;

/// <summary>
/// Encapsulates resolved authorization and department scoping for a user within a tenant.
/// </summary>
public sealed record ResolvedTenantAccess(
    Guid TenantId,
    string Role,
    IReadOnlyCollection<Guid> EffectiveDepartments);

/// <summary>
/// Service contract for resolving and validating tenant membership and access boundaries.
/// Implemented by the Tenants module to decouple host middleware and hubs from internal database contexts.
/// </summary>
public interface ITenantResolutionService
{
    /// <summary>
    /// Resolves active membership and calculates effective department permissions for an authenticated user.
    /// </summary>
    Task<ResolvedTenantAccess?> ResolveTenantAccessAsync(
        Guid tenantId,
        string externalUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether an authenticated user holds active membership within the specified tenant.
    /// </summary>
    Task<bool> HasTenantAccessAsync(
        Guid tenantId,
        string externalUserId,
        CancellationToken cancellationToken = default);
}
