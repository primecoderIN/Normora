namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents a pending invitation for an external user (via email) to join a specific tenant.
/// </summary>
public class TenantInvitation
{
    public Guid Id { get; set; }

    /// <summary>
    /// The email address of the person being invited.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The tenant the user is being invited to join.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// A unique, unguessable token sent in the invitation link.
    /// The user must present this token to accept the invitation.
    /// </summary>
    public Guid Token { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When the invitation expires and can no longer be accepted.
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(48);

    /// <summary>
    /// The current status of the invitation (e.g., "Pending", "Accepted", "Revoked").
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Tenant Tenant { get; set; } = null!;
}
