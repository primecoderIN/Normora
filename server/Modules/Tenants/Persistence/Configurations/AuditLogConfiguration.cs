using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.TargetType).HasMaxLength(100);
        builder.Property(a => a.TargetId).HasMaxLength(100);
        builder.Property(a => a.ActorUserId).HasMaxLength(100);
        builder.Property(a => a.ActorEmail).HasMaxLength(255);
        builder.Property(a => a.ActorName).HasMaxLength(100);

        builder.HasIndex(a => new { a.TenantId, a.OccurredAt });
        builder.HasIndex(a => new { a.TenantId, a.Action });

        builder.HasOne(a => a.Tenant)
              .WithMany()
              .HasForeignKey(a => a.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

