using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class TenantPlanConfiguration : IEntityTypeConfiguration<TenantPlan>
{
    public void Configure(EntityTypeBuilder<TenantPlan> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.TenantId).IsUnique();
        builder.Property(p => p.PlanTier).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Currency).HasMaxLength(10).HasDefaultValue("inr");
        builder.Property(p => p.PaymentTransactionId).HasMaxLength(100);
        builder.Property(p => p.PaymentReferenceId).HasMaxLength(100);
        builder.Property(p => p.SubscriptionStatus).HasMaxLength(50);

        builder.HasOne(p => p.Tenant)
              .WithOne()
              .HasForeignKey<TenantPlan>(p => p.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

