namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Domain Entity representing an immutable, itemized tax invoice for subscription billing.
/// </summary>
public class TenantInvoice : Normora.Shared.Interfaces.ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>Unique human-readable invoice identifier (e.g. INV-2026-11-001).</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public string PlanTier { get; set; } = "Enterprise";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string BillingCycle { get; set; } = "Monthly";
    public string Status { get; set; } = "Paid"; // "Paid", "Pending", "Failed"

    public string? PaymentTransactionId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? TaxId { get; set; }
    public string? BillingAddress { get; set; }

    public Tenant? Tenant { get; set; }
}

