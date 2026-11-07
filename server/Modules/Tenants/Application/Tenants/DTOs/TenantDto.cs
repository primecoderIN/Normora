namespace Normora.Modules.Tenants.Application.Tenants.DTOs;

/// <summary>
/// Data transfer object representing the primary attributes of a tenant workspace.
/// Projected from <see cref="Domain.Tenant"/> upon creation or organization lookup.
/// </summary>
public record TenantDto(
    // Unique identifier of the tenant workspace.
    Guid Id,

    // Display name of the company or organization.
    string Name,

    // Unique vanity URL slug identifying the workspace (e.g. "acme-corp").
    string Slug,

    // Tenant operational lifecycle status (0 = Active, 1 = Suspended, 2 = PendingDeactivation).
    int Status,

    // UTC timestamp when the tenant was provisioned.
    DateTime CreatedAt
);

