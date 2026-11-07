using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the User identity entity.
/// Manages the linkage between Normora's internal user record and the external OIDC identity provider (Keycloak/Auth0).
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Primary Key:
        // Internal surrogate identifier (Guid) used exclusively within Normora's relational schema.
        builder.HasKey(u => u.Id);

        // Decoupled External Identity Subject (sub):
        // Maps to the 'sub' claim issued by Keycloak, Entra ID, or Okta.
        // Capped at 100 characters to accommodate standard UUIDs and base64-encoded provider subjects.
        builder.Property(u => u.ExternalUserId).IsRequired().HasMaxLength(100);

        // Deprecated/Legacy Property Ignored:
        // Ensures backwards-compatibility during migration while preventing duplicate schema columns.
        builder.Ignore(u => u.KeycloakUserId);

        // User Contact & Profile Details:
        // Email address synchronized from the external identity token during JIT provisioning.
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.DisplayName).HasMaxLength(100);

        // Global Unique Identity Index:
        // Guarantees 1:1 mapping between an external identity provider subject and a Normora User record.
        // Crucial for prevent race conditions during just-in-time user creation on login.
        builder.HasIndex(u => u.ExternalUserId).IsUnique();
    }
}

