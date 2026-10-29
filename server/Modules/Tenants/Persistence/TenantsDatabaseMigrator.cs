using Microsoft.EntityFrameworkCore;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>Applies pending EF Core migrations for the Tenants module schema.</summary>
public sealed class TenantsDatabaseMigrator(TenantsDbContext context) : IDatabaseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        context.Database.MigrateAsync(cancellationToken);
}
