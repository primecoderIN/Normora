using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for individual chat Message turns.
/// Records user prompts and streaming AI assistant responses including tool calls and markdown content.
/// </summary>
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // Primary Key:
        builder.HasKey(m => m.Id);

        // Unlimited Text Column:
        // Markdown body containing user inquiry or comprehensive assistant explanation.
        builder.Property(m => m.Content).IsRequired().HasColumnType("text");

        // Conversation History Retrieval Index:
        // Indexed by ConversationId to enable instantaneous retrieval of the chronological message thread.
        builder.HasIndex(m => m.ConversationId);

        // Cascade Deletion:
        // When a user deletes a Conversation thread, all child message records are wiped automatically.
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

