using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Documents.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the DocumentVersion entity.
/// Tracks physical blob storage references in MinIO, extracted plain text, and active versioning.
/// </summary>
public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        // Primary Key:
        builder.HasKey(v => v.Id);

        // MinIO Object Storage Key:
        // Capped at 500 characters to accommodate tenant prefix and deterministic GUID keys (e.g. "{tenantId}/{docId}.pdf").
        builder.Property(v => v.StorageObjectName).IsRequired().HasMaxLength(500);

        // Full Raw Extracted Text:
        // Text extracted by Apache Tika prior to semantic sliding-window chunking.
        builder.Property(v => v.ExtractedText).HasColumnType("text");

        // Relational Parent Link:
        // Deleting a Document parent entity physically cascades and cleans up all associated versions.
        builder.HasOne(v => v.Document)
              .WithMany(d => d.Versions)
              .HasForeignKey(v => v.DocumentId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

