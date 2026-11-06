using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Email).IsRequired().HasMaxLength(255);
        builder.Property(i => i.Status).IsRequired().HasMaxLength(20);

        // The invitation token must be strictly unique globally.
        builder.HasIndex(i => i.Token).IsUnique();

        builder.HasOne(i => i.Tenant)
              .WithMany()
              .HasForeignKey(i => i.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

