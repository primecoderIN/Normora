namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents a user identity synchronized from the external Identity Provider (Keycloak).
/// This is a shadow record used to establish relationships with tenants in this database.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// The unique 'sub' identifier from Keycloak. Used to link incoming JWT tokens to this user.
    /// </summary>
    public string KeycloakUserId { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// A collection of all tenants the user has access to, along with their roles.
    /// </summary>
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}
