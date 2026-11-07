using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.HasKey(m => m.Id);

        // Partial Unique Index for Re-Invitation Safety:
        // Enforces that an active user cannot hold duplicate membership rows within the same tenant.
        // The filter '"RemovedAt" IS NULL' allows previously soft-deleted members to be re-invited
        // without violating uniqueness constraints, preserving complete historical audit trails.
        builder.HasIndex(m => new { m.TenantId, m.UserId })
              .IsUnique()
              .HasFilter("\"RemovedAt\" IS NULL");

        // Relational Mapping:
        // Cascades ensure deleting a Tenant entity removes all associated membership records.
        builder.HasOne(m => m.Tenant)
              .WithMany(t => t.Memberships)
              .HasForeignKey(m => m.TenantId)
              .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
              .WithMany(u => u.Memberships)
              .HasForeignKey(m => m.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Soft-Deletion Global Query Filter:
        // Automatically excludes removed members from all standard queries across the application,
        // while allowing handlers to explicitly call .IgnoreQueryFilters() when auditing or reactivating memberships.
        builder.HasQueryFilter(m => m.RemovedAt == null);
    }
}

