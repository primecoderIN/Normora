using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class UserGroupMembershipConfiguration : IEntityTypeConfiguration<UserGroupMembership>
{
    public void Configure(EntityTypeBuilder<UserGroupMembership> builder)
    {
        builder.HasKey(ugm => new { ugm.TenantMembershipId, ugm.UserGroupId });

        builder.HasOne(ugm => ugm.TenantMembership)
              .WithMany(m => m.UserGroupMemberships)
              .HasForeignKey(ugm => ugm.TenantMembershipId)
              .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ugm => ugm.UserGroup)
              .WithMany(g => g.UserGroupMemberships)
              .HasForeignKey(ugm => ugm.UserGroupId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

