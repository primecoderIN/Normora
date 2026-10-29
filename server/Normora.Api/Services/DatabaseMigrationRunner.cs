using Normora.Shared.Interfaces;

namespace Normora.Api.Services;

/// <summary>Runs registered module migrations sequentially and propagates failures to deployment.</summary>
public sealed class DatabaseMigrationRunner(IEnumerable<IDatabaseMigrator> migrators)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        foreach (var migrator in migrators)
        {
            await migrator.MigrateAsync(cancellationToken);
        }
    }
}
