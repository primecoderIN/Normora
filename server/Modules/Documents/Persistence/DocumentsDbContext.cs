using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Normora.Modules.Documents.Domain;
using Normora.Modules.Documents.Persistence.Configurations;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence;

/// <summary>
/// The primary database context for the Documents module. 
/// Handles all document entities and enforces cross-tenant data isolation.
/// </summary>
public class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Document> Documents { get; set; } = null!;
    // Document Versioning (Phase 19): Tracks immutable file versions within a Document container.
    public DbSet<DocumentVersion> DocumentVersions { get; set; } = null!;
    public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;
    public DbSet<DocumentDepartment> DocumentDepartments { get; set; } = null!;

    /// <summary>
    /// Configures the entity models, setting up constraints, indexes, and global query filters.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("documents");
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);

        modelBuilder.Entity<Document>().HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && d.DeletedAt == null);
        modelBuilder.Entity<DocumentVersion>().HasQueryFilter(v => v.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DocumentChunk>().HasQueryFilter(chunk => chunk.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DocumentDepartment>().HasQueryFilter(dd => dd.TenantId == _tenantContext.TenantId);
    }

    /// <summary>
    /// Overrides standard SaveChanges to inject the active TenantId automatically.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // HTTP callers must not be able to persist an entity under a different tenant than
        // the resolved request context, even if a command contains a caller-supplied ID.
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Added))
        {
            if (_tenantContext.IsTenantResolved && _tenantContext.TenantId.HasValue)
            {
                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
