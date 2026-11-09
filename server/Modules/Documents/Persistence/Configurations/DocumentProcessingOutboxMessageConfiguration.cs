using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Documents.Domain;

namespace Normora.Modules.Documents.Persistence.Configurations;

public sealed class DocumentProcessingOutboxMessageConfiguration : IEntityTypeConfiguration<DocumentProcessingOutboxMessage>
{
    public void Configure(EntityTypeBuilder<DocumentProcessingOutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);
        builder.HasIndex(message => message.DocumentVersionId).IsUnique();
        builder.HasIndex(message => message.CreatedAt);
        builder.HasOne<DocumentVersion>()
            .WithMany()
            .HasForeignKey(message => message.DocumentVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
