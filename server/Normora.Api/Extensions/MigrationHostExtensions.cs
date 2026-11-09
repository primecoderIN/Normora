using Duende.Bff.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Normora.Api.Services;
using Normora.Infrastructure.Extensions;
using Normora.Infrastructure.Services;
using Normora.Shared.Interfaces;

namespace Normora.Api.Extensions;

/// <summary>
/// Dedicated ephemeral host execution for database schema migrations.
/// Isolates schema migration execution from Kestrel, OpenTelemetry, SignalR,
/// and web middleware to prevent container lifecycle crashes (SIGSEGV / exit 139).
/// </summary>
public static class MigrationHostExtensions
{
    public static async Task RunMigrationsAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var migrationBuilder = Host.CreateApplicationBuilder(args);
        migrationBuilder.Services.AddConfigurationServices(migrationBuilder.Configuration);
        migrationBuilder.Services.AddDatabaseServices(
            migrationBuilder.Configuration,
            migrationBuilder.Environment,
            migrationMode: true);
        migrationBuilder.Services.AddScoped<DatabaseMigrationRunner>();

        // Register SessionDbContext strictly for SessionDatabaseMigrator without Duende background processes
        migrationBuilder.Services.AddDbContext<SessionDbContext>(options =>
        {
            options.UseNpgsql(
                migrationBuilder.Configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly("Normora.Api"));
        });
        migrationBuilder.Services.AddScoped<IDatabaseMigrator, SessionDatabaseMigrator>();

        using var migrationHost = migrationBuilder.Build();
        await using var scope = migrationHost.Services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<DatabaseMigrationRunner>();
        await runner.MigrateAsync(cancellationToken);
    }
}

