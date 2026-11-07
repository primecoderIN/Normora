using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the UserGroupMembership join entity.
/// Maps individual tenant memberships into one or more user groups for batch access control.
/// </summary>
public class UserGroupMembershipConfiguration : IEntityTypeConfiguration<UserGroupMembership>
{
    public void Configure(EntityTypeBuilder<UserGroupMembership> builder)
    {
        // Composite Primary Key:
        // Enforces that an employee membership cannot be assigned multiple times to the exact same user group.
        builder.HasKey(ugm => new { ugm.TenantMembershipId, ugm.UserGroupId });

        // Membership Relationship:
        // When a membership is removed or soft-deleted, its association with groups is cleared.
        builder.HasOne(ugm => ugm.TenantMembership)
              .WithMany(m => m.UserGroupMemberships)
              .HasForeignKey(ugm => ugm.TenantMembershipId)
              .OnDelete(DeleteBehavior.Cascade);

        // Group Relationship:
        // Deleting a user group unbinds all assigned members automatically.
        builder.HasOne(ugm => ugm.UserGroup)
              .WithMany(g => g.UserGroupMemberships)
              .HasForeignKey(ugm => ugm.UserGroupId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

