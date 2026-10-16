namespace Normora.Modules.Tenants.Domain;

public enum PlanTier
{
    Free = 0,
    Pro = 1,
    Enterprise = 2
}

/// <summary>
/// Defines the subscription tier and resource limits for a tenant.
/// </summary>
public class TenantPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public PlanTier PlanTier { get; set; } = PlanTier.Free;
    public int MaxDocuments { get; set; } = 10;
    public int MaxEmployees { get; set; } = 5;
    public long MaxStorageBytes { get; set; } = 52_428_800; // 50 MB
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }

    public static TenantPlan CreateDefault(Guid tenantId, PlanTier tier = PlanTier.Free) => tier switch
    {
        PlanTier.Pro => new TenantPlan
        {
            TenantId = tenantId,
            PlanTier = PlanTier.Pro,
            MaxDocuments = 100,
            MaxEmployees = 50,
            MaxStorageBytes = 5_368_709_120 // 5 GB
        },
        PlanTier.Enterprise => new TenantPlan
        {
            TenantId = tenantId,
            PlanTier = PlanTier.Enterprise,
            MaxDocuments = 10_000,
            MaxEmployees = 5_000,
            MaxStorageBytes = 536_870_912_000 // 500 GB
        },
        _ => new TenantPlan
        {
            TenantId = tenantId,
            PlanTier = PlanTier.Free,
            MaxDocuments = 10,
            MaxEmployees = 5,
            MaxStorageBytes = 52_428_800 // 50 MB
        }
    };
}
