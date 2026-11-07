using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

public class SavedAnswerConfiguration : IEntityTypeConfiguration<SavedAnswer>
{
    public void Configure(EntityTypeBuilder<SavedAnswer> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ConversationId);

        // Prevent a user from saving the same message twice
        builder.HasIndex(s => new { s.UserId, s.MessageId }).IsUnique();

        builder.HasOne(s => s.Message)
            .WithMany()
            .HasForeignKey(s => s.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

