using Microsoft.EntityFrameworkCore;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Persistence;

/// <summary>Applies pending EF Core migrations for the Conversations module schema.</summary>
public sealed class ConversationsDatabaseMigrator(ConversationsDbContext context) : IDatabaseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        context.Database.MigrateAsync(cancellationToken);
}
