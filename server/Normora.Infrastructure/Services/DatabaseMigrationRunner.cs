using Microsoft.Extensions.Logging;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Services;

/// <summary>Runs registered module migrations sequentially and propagates failures to deployment.</summary>
public sealed class DatabaseMigrationRunner(
    IEnumerable<IDatabaseMigrator> migrators,
    ILogger<DatabaseMigrationRunner> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        // Sequential Multi-Schema Migration Pipeline:
        // Each bounded module implements IDatabaseMigrator (TenantsDatabaseMigrator, DocumentsDatabaseMigrator,
        // ConversationsDatabaseMigrator) targeting its own dedicated PostgreSQL schema ('tenants', 'documents', 'conversations').
        // The one-off migration process applies each schema sequentially before the deployment starts API replicas.
        // Any failure aborts this process so deployment cannot mistake a partial migration for success.
        logger.LogInformation("Starting database migration runner...");
        var migratorList = migrators.ToList();
        logger.LogInformation("Found {Count} database migrators to execute.", migratorList.Count);

        foreach (var migrator in migratorList)
        {
            var migratorName = migrator.GetType().Name;
            logger.LogInformation("Applying migrations for {MigratorName}...", migratorName);
            await migrator.MigrateAsync(cancellationToken);
            logger.LogInformation("Successfully completed migrations for {MigratorName}.", migratorName);
        }

        logger.LogInformation("All database migrations completed successfully.");
    }
}
