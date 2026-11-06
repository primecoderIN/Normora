namespace Normora.Modules.Tenants.Application.Tenants.DTOs;

/// <summary>
/// Data transfer object representing a tenant after creation.
/// </summary>
public record TenantDto(Guid Id, string Name, string Slug, int Status, DateTime CreatedAt);

