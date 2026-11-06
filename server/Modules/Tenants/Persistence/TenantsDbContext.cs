using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>
/// A specialized database context specifically for managing the multi-tenancy layer.
/// This context handles Tenants, Users, and the many-to-many Memberships between them.
/// It operates independently of the main AppDbContext to ensure a strict boundary.
/// </summary>
public class TenantsDbContext(DbContextOptions<TenantsDbContext> options) : DbContext(options)
{
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
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<TenantPlan> TenantPlans { get; set; } = null!;

    /// <summary>
    /// Configures the relational mappings and strict uniqueness constraints required for multi-tenancy.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("tenants");

        // Automatically discover and apply all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantsDbContext).Assembly);
    }
}
