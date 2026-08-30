namespace Normora.Modules.Tenants.Domain;

public class TenantInvitation
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid Token { get; set; } = Guid.NewGuid();
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Revoked
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Tenant Tenant { get; set; } = null!;
}
