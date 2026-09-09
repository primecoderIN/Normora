using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence;

public class ConversationsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ConversationsDbContext(DbContextOptions<ConversationsDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<MessageCitation> MessageCitations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.TenantId);
            entity.HasIndex(c => c.UserId);
            
            // Global Query Filter for Tenant Data Isolation
            entity.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired().HasColumnType("text");
            entity.HasIndex(m => m.ConversationId);
            
            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            entity.HasQueryFilter(m => m.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<MessageCitation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FileName).HasMaxLength(255);
            entity.HasIndex(c => c.MessageId);
            
            entity.HasOne(c => c.Message)
                .WithMany(m => m.Citations)
                .HasForeignKey(c => c.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter
            entity.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Added))
        {
            if (_tenantContext.IsTenantResolved && _tenantContext.TenantId.HasValue)
            {
                entry.Entity.TenantId = _tenantContext.TenantId.Value;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Conversation>().Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
