using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the UserGroupDepartment join entity.
/// Establishes many-to-many relationships associating security user groups with departmental scopes.
/// </summary>
public class UserGroupDepartmentConfiguration : IEntityTypeConfiguration<UserGroupDepartment>
{
    public void Configure(EntityTypeBuilder<UserGroupDepartment> builder)
    {
        // Composite Primary Key:
        // Guarantees that a user group cannot be associated with the same department more than once.
        builder.HasKey(ugd => new { ugd.UserGroupId, ugd.DepartmentId });

        // User Group Association:
        // Cascades deletion so removing a user group automatically clears its department mappings.
        builder.HasOne(ugd => ugd.UserGroup)
              .WithMany(g => g.UserGroupDepartments)
              .HasForeignKey(ugd => ugd.UserGroupId)
              .OnDelete(DeleteBehavior.Cascade);

        // Department Association:
        // Restricts deletion if departments are still bound to active user groups to prevent accidental privilege breaks.
        builder.HasOne(ugd => ugd.Department)
              .WithMany(d => d.UserGroupDepartments)
              .HasForeignKey(ugd => ugd.DepartmentId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}

