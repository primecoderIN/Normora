using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class TenantBrandingConfiguration : IEntityTypeConfiguration<TenantBranding>
{
    public void Configure(EntityTypeBuilder<TenantBranding> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.PrimaryColor).HasMaxLength(20);
        builder.Property(b => b.SecondaryColor).HasMaxLength(20);
        builder.Property(b => b.LogoUrl).HasMaxLength(500);
        builder.Property(b => b.LogoUrlDark).HasMaxLength(500);
        builder.Property(b => b.FaviconUrl).HasMaxLength(500);

        builder.HasOne(b => b.Tenant)
              .WithOne(t => t.Branding)
              .HasForeignKey<TenantBranding>(b => b.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

