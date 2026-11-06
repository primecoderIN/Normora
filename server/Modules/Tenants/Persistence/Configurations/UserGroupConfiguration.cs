using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Description).HasMaxLength(500);
        builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();

        builder.HasOne(g => g.Tenant)
              .WithMany(t => t.UserGroups)
              .HasForeignKey(g => g.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

