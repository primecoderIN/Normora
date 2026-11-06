using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.HasKey(m => m.Id);

        // TENANT-05: (TenantId, UserId) → UNIQUE for active memberships (partial index)
        builder.HasIndex(m => new { m.TenantId, m.UserId })
              .IsUnique()
              .HasFilter("\"RemovedAt\" IS NULL");

        // TENANT-04: Relationships
        builder.HasOne(m => m.Tenant)
              .WithMany(t => t.Memberships)
              .HasForeignKey(m => m.TenantId)
              .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
              .WithMany(u => u.Memberships)
              .HasForeignKey(m => m.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Phase 21: Soft-delete for memberships
        builder.HasQueryFilter(m => m.RemovedAt == null);
    }
}

