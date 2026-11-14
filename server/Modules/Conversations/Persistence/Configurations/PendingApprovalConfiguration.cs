using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Normora.Modules.Conversations.Domain;

namespace Normora.Modules.Conversations.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the <see cref="PendingApproval"/> entity.
/// Configures table name, column constraints, and lookup indices.
/// </summary>
public class PendingApprovalConfiguration : IEntityTypeConfiguration<PendingApproval>
{
    public void Configure(EntityTypeBuilder<PendingApproval> builder)
    {
        builder.ToTable("PendingApprovals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ToolName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.ArgumentsJson)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.ExpiresAt)
            .IsRequired();

        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.UserId, p.Status });
        builder.HasIndex(p => p.ExpiresAt);
    }
}

