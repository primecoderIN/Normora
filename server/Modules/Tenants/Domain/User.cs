using System.ComponentModel.DataAnnotations.Schema;

namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Domain Entity representing an internal user identity synchronized from an external OIDC Provider.
/// Acts as an internal profile record linking OIDC claims (ExternalUserId, Email, DisplayName)
/// to tenant memberships, departmental permissions, and user groups.
/// Provisioned Just-In-Time (JIT) on the user's first authenticated request.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// The unique 'sub' subject claim from the OIDC identity provider (ExternalUserId).
    /// Bound to the primary unique index for lightning-fast user resolution during token processing.
    /// </summary>
    public string ExternalUserId { get; set; } = string.Empty;

    [NotMapped]
    public string KeycloakUserId
    {
        get => ExternalUserId;
        set => ExternalUserId = value;
    }

    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// A collection of all active and historical tenant memberships held by this user.
    /// </summary>
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}
