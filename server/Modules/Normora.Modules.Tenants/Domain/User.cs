namespace Normora.Modules.Tenants.Domain;

public class User
{
    public Guid Id { get; set; }
    public string KeycloakUserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}
