using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the ConversationShare entity.
/// Manages read-only public or internal share links generated for entire conversation threads.
/// </summary>
public class ConversationShareConfiguration : IEntityTypeConfiguration<ConversationShare>
{
    public void Configure(EntityTypeBuilder<ConversationShare> builder)
    {
        // Primary Key:
        builder.HasKey(s => s.Id);

        // Secure Share Token:
        // High-entropy random token exposed in the shareable URL (e.g. /share/{token}).
        // Capped at 50 characters to accommodate secure cryptographically random hashes.
        builder.Property(s => s.Token).IsRequired().HasMaxLength(50);

        // Global Uniqueness Constraint:
        // Guarantees zero token collisions across all tenants and threads.
        // Backed by a PostgreSQL B-Tree unique index for O(log N) lookup when resolving shared links.
        builder.HasIndex(s => s.Token).IsUnique();

        // Conversation Linkage Index:
        builder.HasIndex(s => s.ConversationId);

        // Cascade Deletion:
        // Deleting the parent conversation immediately revokes and deletes all associated share links.
        builder.HasOne(s => s.Conversation)
            .WithMany()
            .HasForeignKey(s => s.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

