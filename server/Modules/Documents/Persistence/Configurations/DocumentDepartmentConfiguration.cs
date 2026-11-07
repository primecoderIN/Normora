using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Documents.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the DocumentDepartment junction entity.
/// Manages fine-grained departmental ACLs restricting which employees may retrieve given document chunks during RAG queries.
/// </summary>
public class DocumentDepartmentConfiguration : IEntityTypeConfiguration<DocumentDepartment>
{
    public void Configure(EntityTypeBuilder<DocumentDepartment> builder)
    {
        // Composite Primary Key:
        // Ensures a document cannot be bound to the exact same department more than once.
        // DepartmentId is stored as a raw un-navigated Guid to maintain strict modular boundary isolation from Tenants.
        // If a document has ZERO DocumentDepartment junction entries, it is treated as "Company-Wide" (accessible to all members).
        builder.HasKey(dd => new { dd.DocumentId, dd.DepartmentId });

        // Document Association:
        // Hard-deleting or purging a document cascades and purges all of its department scopes.
        builder.HasOne(dd => dd.Document)
              .WithMany(d => d.DocumentDepartments)
              .HasForeignKey(dd => dd.DocumentId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

