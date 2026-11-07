using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the SavedAnswer entity.
/// Manages bookmarked AI answers pinned by employees for offline reference, export, and quick access.
/// </summary>
public class SavedAnswerConfiguration : IEntityTypeConfiguration<SavedAnswer>
{
    public void Configure(EntityTypeBuilder<SavedAnswer> builder)
    {
        // Primary Key:
        builder.HasKey(s => s.Id);

        // User Filtering Index:
        // Powers the "Saved Answers" tab in the employee portal, indexing by UserId for fast personal retrieval.
        builder.HasIndex(s => s.UserId);

        // Thread Association Index:
        builder.HasIndex(s => s.ConversationId);

        // Idempotent Bookmark Uniqueness:
        // Enforces that an employee cannot bookmark the exact same assistant response more than once.
        // Prevents UI duplication bugs and redundant database rows.
        builder.HasIndex(s => new { s.UserId, s.MessageId }).IsUnique();

        // Message Linkage & Cascade:
        // If the original message is purged (e.g. thread deletion), the saved answer reference is automatically removed.
        builder.HasOne(s => s.Message)
            .WithMany()
            .HasForeignKey(s => s.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

