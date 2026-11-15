using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class TenantBillingDetailConfiguration : IEntityTypeConfiguration<TenantBillingDetail>
{
    public void Configure(EntityTypeBuilder<TenantBillingDetail> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => b.TenantId).IsUnique();

        builder.Property(b => b.BillingEmail).HasMaxLength(200);
        builder.Property(b => b.CompanyLegalName).HasMaxLength(200);
        builder.Property(b => b.TaxId).HasMaxLength(100);
        builder.Property(b => b.BillingAddress).HasMaxLength(500);

        builder.HasOne(b => b.Tenant)
            .WithOne()
            .HasForeignKey<TenantBillingDetail>(b => b.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

