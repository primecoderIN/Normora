using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Documents.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence.Configurations;

public class DocumentDepartmentConfiguration(ITenantContext tenantContext) : IEntityTypeConfiguration<DocumentDepartment>
{
    public void Configure(EntityTypeBuilder<DocumentDepartment> builder)
    {
        // DocumentDepartment — junction table for document department scoping.
        // DepartmentId is a plain Guid FK (no EF nav) to respect the module boundary.
        // A document with zero DocumentDepartment rows is treated as "Company Wide".
        builder.HasKey(dd => new { dd.DocumentId, dd.DepartmentId });

        builder.HasOne(dd => dd.Document)
              .WithMany(d => d.DocumentDepartments)
              .HasForeignKey(dd => dd.DocumentId)
              .OnDelete(DeleteBehavior.Cascade);

        // The matching query filter on TenantId aligns with the Document filter to
        // prevent EF Core validation warning EF10622 on required-end relationships.
        builder.HasQueryFilter(dd => dd.TenantId == tenantContext.TenantId);
    }
}

