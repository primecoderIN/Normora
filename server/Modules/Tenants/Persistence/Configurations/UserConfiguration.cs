using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.ExternalUserId).IsRequired().HasMaxLength(100);
        builder.Ignore(u => u.KeycloakUserId);
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.DisplayName).HasMaxLength(100);

        // TENANT-05: User.ExternalUserId → UNIQUE
        builder.HasIndex(u => u.ExternalUserId).IsUnique();
    }
}

