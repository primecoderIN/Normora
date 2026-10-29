namespace Normora.Shared.Interfaces;

/// <summary>
/// Applies schema migrations for a persistence boundary owned by its registering module.
/// </summary>
public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
