using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

public class MessageCitationConfiguration : IEntityTypeConfiguration<MessageCitation>
{
    public void Configure(EntityTypeBuilder<MessageCitation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FileName).HasMaxLength(255);

        // Fast retrieval index for rendering citation references alongside chat messages
        builder.HasIndex(c => c.MessageId);

        // Cross-Module Boundary Isolation:
        // DocumentId and DocumentChunkId are stored as unnavigated GUIDs rather than EF Core navigation properties.
        // This ensures the Conversations DbContext does not couple to or reference the Documents schema.

        // Cascade delete ensures deleting an assistant message purges its citation references
        builder.HasOne(c => c.Message)
            .WithMany(m => m.Citations)
            .HasForeignKey(c => c.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

