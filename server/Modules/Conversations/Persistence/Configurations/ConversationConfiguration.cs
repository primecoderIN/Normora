using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence.Configurations;

public class ConversationConfiguration(ITenantContext tenantContext) : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.UserId);

        // Global Query Filter for Tenant Data Isolation
        builder.HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
    }
}

