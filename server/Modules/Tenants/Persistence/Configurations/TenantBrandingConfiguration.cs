using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the TenantBranding entity.
/// Stores white-label styling assets (hex color codes, light/dark logos, favicons) applied to the Angular client.
/// </summary>
public class TenantBrandingConfiguration : IEntityTypeConfiguration<TenantBranding>
{
    public void Configure(EntityTypeBuilder<TenantBranding> builder)
    {
        // Primary Key:
        builder.HasKey(b => b.Id);

        // Hex Color Schemes:
        // Capped to 20 characters to store CSS hex strings (e.g. "#4F46E5" or "rgb(...)").
        builder.Property(b => b.PrimaryColor).HasMaxLength(20);
        builder.Property(b => b.SecondaryColor).HasMaxLength(20);

        // Asset URLs:
        // URLs pointing to MinIO or external CDN hosting company logos and browser favicons.
        builder.Property(b => b.LogoUrl).HasMaxLength(500);
        builder.Property(b => b.LogoUrlDark).HasMaxLength(500);
        builder.Property(b => b.FaviconUrl).HasMaxLength(500);

        // 1:1 Relationship with Tenant:
        // Ensures each organization maintains exactly one active white-label branding configuration.
        builder.HasOne(b => b.Tenant)
              .WithOne(t => t.Branding)
              .HasForeignKey<TenantBranding>(b => b.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

