using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the UserGroup entity.
/// Groups provide batch role assignment and departmental permission propagation for enterprise tenants.
/// </summary>
public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        // Primary Key:
        builder.HasKey(g => g.Id);

        // Group Title & Description:
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Description).HasMaxLength(500);

        // Tenant-Scoped Uniqueness:
        // Ensures user group names are distinct per tenant while allowing identical names across separate tenants.
        builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();

        // Foreign Key & Cascade Deletion:
        // Removing an organization permanently deletes its custom user security groups.
        builder.HasOne(g => g.Tenant)
              .WithMany(t => t.UserGroups)
              .HasForeignKey(g => g.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

