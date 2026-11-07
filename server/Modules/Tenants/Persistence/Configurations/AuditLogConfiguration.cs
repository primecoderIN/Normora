using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the immutable AuditLog entity.
/// Records security events, document updates, member changes, and administrative actions for enterprise compliance.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Primary Key:
        builder.HasKey(a => a.Id);

        // Action & Target Metadata:
        // Action identifies the command (e.g. "EmployeeInvited", "DocumentDeleted", "RoleChanged").
        // TargetType and TargetId describe the affected entity for cross-referencing.
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.TargetType).HasMaxLength(100);
        builder.Property(a => a.TargetId).HasMaxLength(100);

        // Actor Attribution:
        // Captures snapshots of the initiating user's email, name, and subject identifier at event time,
        // ensuring immutable accountability even if the user is later modified or removed.
        builder.Property(a => a.ActorUserId).HasMaxLength(100);
        builder.Property(a => a.ActorEmail).HasMaxLength(255);
        builder.Property(a => a.ActorName).HasMaxLength(100);

        // Composite Indexing for Compliance Reporting:
        // Fast time-series querying per tenant (e.g. chronological audit feeds on employer dashboards).
        builder.HasIndex(a => new { a.TenantId, a.OccurredAt });

        // Action Filtering Index:
        // Fast filtering by specific security action types within a tenant's compliance log.
        builder.HasIndex(a => new { a.TenantId, a.Action });

        // Multi-Tenant Isolation & Cascade:
        // Deleting a Tenant purges its associated audit trail.
        builder.HasOne(a => a.Tenant)
              .WithMany()
              .HasForeignKey(a => a.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

