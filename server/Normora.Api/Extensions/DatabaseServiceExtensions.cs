using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using Normora.Modules.Tenants.Persistence;
using Normora.Modules.Documents.Persistence;
using Minio;

namespace Normora.Api.Extensions;

/// <summary>
/// Registers the PostgreSQL database contexts and MinIO object storage services.
/// Note that under a Modular Monolith architecture, each module manages its own distinct DbContext.
/// </summary>
public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TenantsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContext<DocumentsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        // Configure MinIO S3-compatible storage
        var minioEndpoint = configuration["Minio:Endpoint"] ?? "localhost:9000";
        var minioAccessKey = configuration["Minio:AccessKey"] ?? "admin";
        var minioSecretKey = configuration["Minio:SecretKey"] ?? "password";

        services.AddMinio(configureClient => configureClient
            .WithEndpoint(minioEndpoint)
            .WithCredentials(minioAccessKey, minioSecretKey)
            .WithSSL(false)
            .Build());

        services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();

        return services;
    }
}
