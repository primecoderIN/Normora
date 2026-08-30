using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>
/// A specialized database context specifically for managing the multi-tenancy layer.
/// This context handles Tenants, Users, and the many-to-many Memberships between them.
/// It operates independently of the main AppDbContext to ensure a strict boundary.
/// </summary>
public class TenantsDbContext : DbContext
{
    public TenantsDbContext(DbContextOptions<TenantsDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TenantMembership> TenantMemberships { get; set; } = null!;
    public DbSet<TenantInvitation> TenantInvitations { get; set; } = null!;

    /// <summary>
    /// Configures the relational mappings and strict uniqueness constraints required for multi-tenancy.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Tenant Configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Slug).IsRequired().HasMaxLength(50);
            
            // TENANT-05: Tenant.Slug → UNIQUE
            entity.HasIndex(t => t.Slug).IsUnique();
        });

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.KeycloakUserId).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(255);
            entity.Property(u => u.DisplayName).HasMaxLength(100);

            // TENANT-05: User.KeycloakUserId → UNIQUE
            entity.HasIndex(u => u.KeycloakUserId).IsUnique();
        });

        // TenantMembership Configuration
        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.HasKey(m => m.Id);
            
            // TENANT-05: (TenantId, UserId) → UNIQUE
            entity.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();

            // TENANT-04: Relationships
            entity.HasOne(m => m.Tenant)
                  .WithMany(t => t.Memberships)
                  .HasForeignKey(m => m.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                  .WithMany(u => u.Memberships)
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TenantInvitation Configuration
        modelBuilder.Entity<TenantInvitation>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Email).IsRequired().HasMaxLength(255);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(20);

            // The invitation token must be strictly unique globally.
            entity.HasIndex(i => i.Token).IsUnique();

            // A single email shouldn't have multiple pending invitations for the same tenant.
            // But they can have revoked/accepted ones. So a composite index could be useful, 
            // though not strictly unique enforced at DB level without condition.

            entity.HasOne(i => i.Tenant)
                  .WithMany()
                  .HasForeignKey(i => i.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
