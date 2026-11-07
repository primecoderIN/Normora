using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the TenantPlan entity.
/// Manages billing tier assignments, payment gateway references (PayU), and document storage quotas.
/// </summary>
public class TenantPlanConfiguration : IEntityTypeConfiguration<TenantPlan>
{
    public void Configure(EntityTypeBuilder<TenantPlan> builder)
    {
        // Primary Key:
        builder.HasKey(p => p.Id);

        // 1:1 Mapping Guarantee:
        // A tenant may have exactly one active subscription plan record at any given point in time.
        // Backed by a unique index on TenantId to avoid multiple active billing records.
        builder.HasIndex(p => p.TenantId).IsUnique();

        // Enum String Conversion:
        // Converts PlanTier enum (Free, Starter, Professional, Enterprise) to a human-readable varchar(20)
        // to simplify raw SQL reporting and guard against enum integer drift.
        builder.Property(p => p.PlanTier).HasConversion<string>().HasMaxLength(20);

        // Payment Processing Metadata:
        // Currency defaults to Indian Rupee ("inr") as required by PayU payment gateway integrations.
        builder.Property(p => p.Currency).HasMaxLength(10).HasDefaultValue("inr");
        builder.Property(p => p.PaymentTransactionId).HasMaxLength(100);
        builder.Property(p => p.PaymentReferenceId).HasMaxLength(100);
        builder.Property(p => p.SubscriptionStatus).HasMaxLength(50);

        // Foreign Key Relationship:
        // Cascades from the root Tenant entity upon tenant deletion.
        builder.HasOne(p => p.Tenant)
              .WithOne()
              .HasForeignKey<TenantPlan>(p => p.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

