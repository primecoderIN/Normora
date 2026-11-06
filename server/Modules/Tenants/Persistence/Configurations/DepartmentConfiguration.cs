using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.HasIndex(d => new { d.TenantId, d.Name }).IsUnique();

        builder.HasOne(d => d.Tenant)
              .WithMany(t => t.Departments)
              .HasForeignKey(d => d.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

