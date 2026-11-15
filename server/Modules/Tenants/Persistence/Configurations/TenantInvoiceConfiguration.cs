using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class TenantInvoiceConfiguration : IEntityTypeConfiguration<TenantInvoice>
{
    public void Configure(EntityTypeBuilder<TenantInvoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.PlanTier).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(10).HasDefaultValue("INR");
        builder.Property(i => i.BillingCycle).HasMaxLength(20).HasDefaultValue("Monthly");
        builder.Property(i => i.Status).HasMaxLength(30).HasDefaultValue("Paid");
        builder.Property(i => i.PaymentTransactionId).HasMaxLength(100);
        builder.Property(i => i.CustomerName).HasMaxLength(200);
        builder.Property(i => i.CustomerEmail).HasMaxLength(200);
        builder.Property(i => i.TaxId).HasMaxLength(100);
        builder.Property(i => i.BillingAddress).HasMaxLength(500);

        builder.Property(i => i.Amount).HasPrecision(18, 2);

        builder.HasOne(i => i.Tenant)
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

