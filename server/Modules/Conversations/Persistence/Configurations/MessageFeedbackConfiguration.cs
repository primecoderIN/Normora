using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the MessageFeedback entity.
/// Collects user thumbs up / thumbs down quality evaluations and comments on AI responses to measure RAG accuracy.
/// </summary>
public class MessageFeedbackConfiguration : IEntityTypeConfiguration<MessageFeedback>
{
    public void Configure(EntityTypeBuilder<MessageFeedback> builder)
    {
        // Primary Key:
        builder.HasKey(f => f.Id);

        // Feedback Explanation:
        // Optional user comment explaining why an answer was unhelpful (e.g. outdated policy, wrong deductible).
        // Capped at 500 characters to keep feedback concise.
        builder.Property(f => f.Comment).HasMaxLength(500);

        // Idempotent Feedback Uniqueness:
        // Enforces that an employee can record at most one feedback record per assistant message.
        // If an employee updates their rating (e.g. changes thumbs down to thumbs up), it updates the existing entry.
        builder.HasIndex(f => new { f.UserId, f.MessageId }).IsUnique();

        // Cascade Deletion:
        // Removing a parent message automatically deletes associated feedback records.
        builder.HasOne(f => f.Message)
            .WithMany(m => m.Feedbacks)
            .HasForeignKey(f => f.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

