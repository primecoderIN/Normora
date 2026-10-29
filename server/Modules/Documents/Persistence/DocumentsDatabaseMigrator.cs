using Microsoft.EntityFrameworkCore;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence;

/// <summary>Applies pending EF Core migrations for the Documents module schema.</summary>
public sealed class DocumentsDatabaseMigrator(DocumentsDbContext context) : IDatabaseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        context.Database.MigrateAsync(cancellationToken);
}
