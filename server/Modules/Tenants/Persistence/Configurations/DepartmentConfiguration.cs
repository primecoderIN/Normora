using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the Department entity.
/// Defines departmental boundaries used for access scoping across documents and user group memberships.
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        // Primary Key:
        builder.HasKey(d => d.Id);

        // Department Name & Description:
        // Capped to enforce clean directory display in employer dashboards and document filter dropdowns.
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Description).HasMaxLength(500);

        // Tenant-Scoped Uniqueness:
        // Enforces unique department names within a single organization (e.g. only one "Human Resources" per tenant)
        // while allowing different tenants to share standard departmental naming conventions.
        builder.HasIndex(d => new { d.TenantId, d.Name }).IsUnique();

        // Foreign Key & Cascade Behavior:
        // Deleting a Tenant cascades and removes all associated organizational departments.
        builder.HasOne(d => d.Tenant)
              .WithMany(t => t.Departments)
              .HasForeignKey(d => d.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

