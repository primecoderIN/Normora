using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Documents.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence.Configurations;

public class DocumentConfiguration(ITenantContext tenantContext) : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255);

        // Ensure TenantId and DeletedAt are indexed together for fast filtered document queries
        builder.HasIndex(d => new { d.TenantId, d.DeletedAt });

        // TENANT-14: Global Query Filter for Tenant Data Isolation
        // Soft-delete: documents with a non-null DeletedAt are excluded from all queries.
        // The PurgeDeletedDocumentsJob uses IgnoreQueryFilters() to process them.
        builder.HasQueryFilter(d => d.TenantId == tenantContext.TenantId && d.DeletedAt == null);
    }
}

