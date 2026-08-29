using Microsoft.EntityFrameworkCore;
using Normora.Shared;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure;

/// <summary>
/// The primary database context for the application. 
/// Handles all business entities (like Documents) and enforces cross-tenant data isolation.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Document> Documents { get; set; } = null!;

    /// <summary>
    /// Configures the entity models, setting up constraints, indexes, and global query filters.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Ensure TenantId is indexed for faster document queries
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.TenantId);
            
        // Enforce database constraints previously handled by Data Annotations
        modelBuilder.Entity<Document>(entity => 
        {
            entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
            entity.Property(d => d.MinioObjectName).IsRequired().HasMaxLength(500);
            
            // TENANT-14: Global Query Filter for Tenant Data Isolation
            // This ensures that EVERY LINQ query executed against Documents will transparently have
            // `WHERE TenantId = @currentTenantId` appended to it, preventing cross-tenant data leakage.
            entity.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
        });
    }

    /// <summary>
    /// Overrides standard SaveChanges to inject the active TenantId automatically.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Intercept any newly added Document entities...
        foreach (var entry in ChangeTracker.Entries<Document>().Where(e => e.State == EntityState.Added))
        {
            // And automatically assign the TenantId from the current HTTP context!
            if (_tenantContext.IsTenantResolved && _tenantContext.TenantId.HasValue)
            {
                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
