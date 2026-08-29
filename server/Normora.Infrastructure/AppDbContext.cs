using Microsoft.EntityFrameworkCore;
using Normora.Shared;

namespace Normora.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Ensure EmployerId is indexed for faster document queries
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.EmployerId);
            
        // Enforce database constraints previously handled by Data Annotations
        modelBuilder.Entity<Document>(entity => 
        {
            entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
            entity.Property(d => d.MinioObjectName).IsRequired().HasMaxLength(500);
            entity.Property(d => d.EmployerId).IsRequired().HasMaxLength(100);
        });
    }
}
