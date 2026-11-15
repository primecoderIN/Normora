namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Domain Entity storing legal invoicing and contact preferences for a workspace tenant.
/// </summary>
public class TenantBillingDetail : Normora.Shared.Interfaces.ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public string BillingEmail { get; set; } = string.Empty;
    public string CompanyLegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}

