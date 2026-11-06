using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class UserGroupDepartmentConfiguration : IEntityTypeConfiguration<UserGroupDepartment>
{
    public void Configure(EntityTypeBuilder<UserGroupDepartment> builder)
    {
        builder.HasKey(ugd => new { ugd.UserGroupId, ugd.DepartmentId });

        builder.HasOne(ugd => ugd.UserGroup)
              .WithMany(g => g.UserGroupDepartments)
              .HasForeignKey(ugd => ugd.UserGroupId)
              .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ugd => ugd.Department)
              .WithMany(d => d.UserGroupDepartments)
              .HasForeignKey(ugd => ugd.DepartmentId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}

