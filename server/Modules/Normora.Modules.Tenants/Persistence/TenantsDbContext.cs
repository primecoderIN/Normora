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
    public DbSet<TenantBranding> TenantBrandings { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<UserGroup> UserGroups { get; set; } = null!;
    public DbSet<MembershipDepartment> MembershipDepartments { get; set; } = null!;
    public DbSet<UserGroupMembership> UserGroupMemberships { get; set; } = null!;
    public DbSet<UserGroupDepartment> UserGroupDepartments { get; set; } = null!;

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

        // TenantBranding Configuration — 1-to-1 with Tenant
        modelBuilder.Entity<TenantBranding>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.PrimaryColor).HasMaxLength(20);
            entity.Property(b => b.SecondaryColor).HasMaxLength(20);
            entity.Property(b => b.LogoUrl).HasMaxLength(500);
            entity.Property(b => b.FaviconUrl).HasMaxLength(500);

            entity.HasOne(b => b.Tenant)
                  .WithOne(t => t.Branding)
                  .HasForeignKey<TenantBranding>(b => b.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Department — organizational unit scoped to a tenant
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Description).HasMaxLength(500);
            entity.HasIndex(d => new { d.TenantId, d.Name }).IsUnique();

            entity.HasOne(d => d.Tenant)
                  .WithMany(t => t.Departments)
                  .HasForeignKey(d => d.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserGroup — named group of users within a tenant
        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Description).HasMaxLength(500);
            entity.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();

            entity.HasOne(g => g.Tenant)
                  .WithMany(t => t.UserGroups)
                  .HasForeignKey(g => g.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MembershipDepartment — direct user → department assignment
        modelBuilder.Entity<MembershipDepartment>(entity =>
        {
            entity.HasKey(md => new { md.TenantMembershipId, md.DepartmentId });

            entity.HasOne(md => md.TenantMembership)
                  .WithMany(m => m.MembershipDepartments)
                  .HasForeignKey(md => md.TenantMembershipId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(md => md.Department)
                  .WithMany(d => d.MembershipDepartments)
                  .HasForeignKey(md => md.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // UserGroupMembership — user → group membership
        modelBuilder.Entity<UserGroupMembership>(entity =>
        {
            entity.HasKey(ugm => new { ugm.TenantMembershipId, ugm.UserGroupId });

            entity.HasOne(ugm => ugm.TenantMembership)
                  .WithMany(m => m.UserGroupMemberships)
                  .HasForeignKey(ugm => ugm.TenantMembershipId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ugm => ugm.UserGroup)
                  .WithMany(g => g.UserGroupMemberships)
                  .HasForeignKey(ugm => ugm.UserGroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserGroupDepartment — group → department mapping (department inheritance)
        modelBuilder.Entity<UserGroupDepartment>(entity =>
        {
            entity.HasKey(ugd => new { ugd.UserGroupId, ugd.DepartmentId });

            entity.HasOne(ugd => ugd.UserGroup)
                  .WithMany(g => g.UserGroupDepartments)
                  .HasForeignKey(ugd => ugd.UserGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ugd => ugd.Department)
                  .WithMany(d => d.UserGroupDepartments)
                  .HasForeignKey(ugd => ugd.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
