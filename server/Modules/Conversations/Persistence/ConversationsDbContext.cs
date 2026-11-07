using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence.Configurations;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence;

/// <summary>Persists conversation aggregates and enforces tenant filters for tenant-owned records.</summary>
public class ConversationsDbContext(DbContextOptions<ConversationsDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<MessageCitation> MessageCitations { get; set; } = null!;
    public DbSet<SavedAnswer> SavedAnswers { get; set; } = null!;
    public DbSet<MessageFeedback> MessageFeedbacks { get; set; } = null!;
    public DbSet<ConversationShare> ConversationShares { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("conversations");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConversationsDbContext).Assembly);

        modelBuilder.Entity<Conversation>().HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Message>().HasQueryFilter(m => m.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<MessageCitation>().HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<SavedAnswer>().HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<MessageFeedback>().HasQueryFilter(f => f.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ConversationShare>().HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
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
