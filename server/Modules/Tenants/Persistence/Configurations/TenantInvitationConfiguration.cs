using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the TenantInvitation entity.
/// Manages pending invitations issued to prospective employees with secure cryptographically random tokens.
/// </summary>
public class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        // Primary Key:
        builder.HasKey(i => i.Id);

        // Recipient Email & Lifecycle Status:
        // Status tracks states such as "Pending", "Accepted", "Expired", or "Revoked".
        builder.Property(i => i.Email).IsRequired().HasMaxLength(255);
        builder.Property(i => i.Status).IsRequired().HasMaxLength(20);

        // Cryptographic Token Uniqueness:
        // The invitation token is sent via email and validated without requiring prior authentication.
        // It must be globally unique across all tenants to prevent unauthorized token collision attacks.
        builder.HasIndex(i => i.Token).IsUnique();

        // Foreign Key Relationship:
        // Cascades with the tenant so that de-provisioning an organization invalidates all pending invites.
        builder.HasOne(i => i.Tenant)
              .WithMany()
              .HasForeignKey(i => i.TenantId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

