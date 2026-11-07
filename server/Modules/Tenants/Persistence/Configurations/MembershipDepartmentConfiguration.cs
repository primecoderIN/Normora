using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the MembershipDepartment join entity.
/// Directly assigns an employee's TenantMembership to specific organizational departments.
/// </summary>
public class MembershipDepartmentConfiguration : IEntityTypeConfiguration<MembershipDepartment>
{
    public void Configure(EntityTypeBuilder<MembershipDepartment> builder)
    {
        // Composite Primary Key:
        // Guarantees an employee is mapped to a department at most once per tenant.
        builder.HasKey(md => new { md.TenantMembershipId, md.DepartmentId });

        // Membership Relationship:
        // Soft or hard deletion of an employee membership cascades and removes departmental links.
        builder.HasOne(md => md.TenantMembership)
              .WithMany(m => m.MembershipDepartments)
              .HasForeignKey(md => md.TenantMembershipId)
              .OnDelete(DeleteBehavior.Cascade);

        // Department Relationship:
        // Restricts deletion of a department if employees remain directly assigned to it.
        builder.HasOne(md => md.Department)
              .WithMany(d => d.MembershipDepartments)
              .HasForeignKey(md => md.DepartmentId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}

