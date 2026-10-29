using Duende.Bff.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Normora.Shared.Interfaces;

namespace Normora.Api.Services;

/// <summary>Applies pending EF Core migrations for Duende BFF server-side sessions.</summary>
public sealed class SessionDatabaseMigrator(SessionDbContext context) : IDatabaseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        context.Database.MigrateAsync(cancellationToken);
}
