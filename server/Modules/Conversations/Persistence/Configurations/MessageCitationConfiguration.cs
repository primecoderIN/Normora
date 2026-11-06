using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

public class MessageCitationConfiguration(ITenantContext tenantContext) : IEntityTypeConfiguration<MessageCitation>
{
    public void Configure(EntityTypeBuilder<MessageCitation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FileName).HasMaxLength(255);
        builder.HasIndex(c => c.MessageId);

        builder.HasOne(c => c.Message)
            .WithMany(m => m.Citations)
            .HasForeignKey(c => c.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global Query Filter for Tenant Data Isolation
        builder.HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
    }
}

