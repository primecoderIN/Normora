using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Documents.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the Document entity.
/// Manages uploaded company policies, employee handbooks, and compliance contracts.
/// </summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        // Primary Key:
        builder.HasKey(d => d.Id);

        // File Display Name:
        // Capped at 255 characters to reflect standard POSIX and NTFS file system naming constraints.
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255);

        // Composite Multi-Tenant Soft-Delete Index:
        // Highly optimized compound index for queries filtering by TenantId and soft-deletion state (DeletedAt IS NULL).
        // Powers employer document listings and ensures RAG searches never scan deleted tenant assets.
        builder.HasIndex(d => new { d.TenantId, d.DeletedAt });
    }
}

