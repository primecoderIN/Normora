using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Normora.Shared;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence;

/// <summary>
/// The primary database context for the Documents module. 
/// Handles all document entities and enforces cross-tenant data isolation.
/// </summary>
public class DocumentsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

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
        modelBuilder.HasPostgresExtension("vector");
        
        // Ensure TenantId is indexed for faster document queries
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.TenantId);
            
        // Enforce database constraints previously handled by Data Annotations
        modelBuilder.Entity<Document>(entity => 
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
            
            // TENANT-14: Global Query Filter for Tenant Data Isolation
            entity.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<DocumentVersion>(entity => 
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.MinioObjectName).IsRequired().HasMaxLength(500);
            entity.Property(v => v.ExtractedText).HasColumnType("text");
            
            entity.HasOne(v => v.Document)
                  .WithMany(d => d.Versions)
                  .HasForeignKey(v => v.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Document Versioning (Phase 19): The same tenant filter applies to versions so
            // a different tenant cannot read version metadata via IgnoreQueryFilters bypass.
            entity.HasQueryFilter(v => v.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Content).IsRequired().HasColumnType("text");
            entity.Property(chunk => chunk.Embedding).HasColumnType("vector(768)");
            
            // Configure full-text search vector
            entity.HasGeneratedTsVectorColumn(
                chunk => chunk.SearchVector!,
                "english",
                chunk => new { chunk.Content })
            .HasIndex(chunk => chunk.SearchVector)
            .HasMethod("GIN");

            entity.HasIndex(chunk => new { chunk.TenantId, chunk.DocumentVersionId, chunk.ChunkIndex }).IsUnique();
            entity.HasOne<DocumentVersion>()
                .WithMany(v => v.Chunks)
                .HasForeignKey(chunk => chunk.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            // Chunks inherit the same tenant boundary as their parent document; background
            // jobs may bypass this filter only after checking both tenant and document IDs.
            entity.HasQueryFilter(chunk => chunk.TenantId == _tenantContext.TenantId);
        });

        // DocumentDepartment — junction table for document department scoping.
        // DepartmentId is a plain Guid FK (no EF nav) to respect the module boundary.
        // A document with zero DocumentDepartment rows is treated as "Company Wide".
        // The matching query filter on TenantId aligns with the Document filter to
        // prevent EF Core validation warning EF10622 on required-end relationships.
        modelBuilder.Entity<DocumentDepartment>(entity =>
        {
            entity.HasKey(dd => new { dd.DocumentId, dd.DepartmentId });

            entity.HasOne(dd => dd.Document)
                  .WithMany(d => d.DocumentDepartments)
                  .HasForeignKey(dd => dd.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(dd => dd.TenantId == _tenantContext.TenantId);
        });
    }

    /// <summary>
    /// Overrides standard SaveChanges to inject the active TenantId automatically.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // HTTP callers must not be able to persist a document under a different tenant than
        // the resolved request context, even if a command contains a caller-supplied ID.
        foreach (var entry in ChangeTracker.Entries<Document>().Where(e => e.State == EntityState.Added))
        {
            // And automatically assign the TenantId from the current HTTP context!
            if (_tenantContext.IsTenantResolved && _tenantContext.TenantId.HasValue)
            {
                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }

        // Propagate TenantId to DocumentDepartment rows to keep the query filter aligned.
        foreach (var entry in ChangeTracker.Entries<DocumentDepartment>().Where(e => e.State == EntityState.Added))
        {
            if (_tenantContext.IsTenantResolved && _tenantContext.TenantId.HasValue)
            {
                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }

        // Document Versioning (Phase 19): Propagate TenantId to DocumentVersion rows.
        foreach (var entry in ChangeTracker.Entries<DocumentVersion>().Where(e => e.State == EntityState.Added))
        {
            if (_tenantContext.IsTenantResolved && _tenantContext.TenantId.HasValue)
            {
                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
