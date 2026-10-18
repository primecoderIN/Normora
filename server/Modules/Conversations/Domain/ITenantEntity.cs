namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Enforces multi-tenant data partitioning across conversation entities.
/// Allows global query filters and DbContext interceptors to apply automatic tenant scoping.
/// </summary>
public interface ITenantEntity
{
    /// <summary>The tenant identifier to which the record belongs.</summary>
    Guid TenantId { get; set; }
}

