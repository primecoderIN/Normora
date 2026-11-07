using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

public class MessageFeedbackConfiguration : IEntityTypeConfiguration<MessageFeedback>
{
    public void Configure(EntityTypeBuilder<MessageFeedback> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Comment).HasMaxLength(500);

        // One feedback per user per message
        builder.HasIndex(f => new { f.UserId, f.MessageId }).IsUnique();

        builder.HasOne(f => f.Message)
            .WithMany(m => m.Feedbacks)
            .HasForeignKey(f => f.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

