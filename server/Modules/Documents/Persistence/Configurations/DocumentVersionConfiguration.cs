using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Documents.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.StorageObjectName).IsRequired().HasMaxLength(500);
        builder.Property(v => v.ExtractedText).HasColumnType("text");

        builder.HasOne(v => v.Document)
              .WithMany(d => d.Versions)
              .HasForeignKey(v => v.DocumentId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}

