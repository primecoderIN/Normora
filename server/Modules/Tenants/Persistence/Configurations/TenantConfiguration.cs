using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the root Tenant entity.
/// Configures primary key, column constraints, and global uniqueness rules for tenant identification.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Primary Key:
        // GUID-based identifier generated upon organization creation.
        // Serves as the partition key across all multi-tenant data stores in Normora.
        builder.HasKey(t => t.Id);

        // Display Name:
        // Human-readable organization name (e.g. "Acme Corp").
        // Restricted to 100 characters to prevent database bloat and UI layout overflow.
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        // URL Slug & Route Identifier:
        // Lowercase alphanumeric identifier used for vanity URLs, subdomains, and ambient tenant matching.
        // Maximum length of 50 characters prevents unwieldy URL structures.
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(50);

        // Unique Constraint on Tenant Slug:
        // Guarantees that no two organizations can claim the same route identifier or subdomain.
        // Backed by a PostgreSQL B-Tree unique index for O(log N) lookup during routing resolution.
        builder.HasIndex(t => t.Slug).IsUnique();
    }
}

