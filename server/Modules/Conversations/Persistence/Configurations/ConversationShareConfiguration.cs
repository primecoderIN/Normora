using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

public class ConversationShareConfiguration : IEntityTypeConfiguration<ConversationShare>
{
    public void Configure(EntityTypeBuilder<ConversationShare> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Token).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Token).IsUnique();
        builder.HasIndex(s => s.ConversationId);

        builder.HasOne(s => s.Conversation)
            .WithMany()
            .HasForeignKey(s => s.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

