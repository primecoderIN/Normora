using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Persistence.Configurations;

public class MembershipDepartmentConfiguration : IEntityTypeConfiguration<MembershipDepartment>
{
    public void Configure(EntityTypeBuilder<MembershipDepartment> builder)
    {
        builder.HasKey(md => new { md.TenantMembershipId, md.DepartmentId });

        builder.HasOne(md => md.TenantMembership)
              .WithMany(m => m.MembershipDepartments)
              .HasForeignKey(md => md.TenantMembershipId)
              .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(md => md.Department)
              .WithMany(d => d.MembershipDepartments)
              .HasForeignKey(md => md.DepartmentId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}

