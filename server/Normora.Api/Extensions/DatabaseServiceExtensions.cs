using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using Minio;
using Normora.Modules.Conversations;
using Normora.Modules.Documents;
using Normora.Modules.Tenants;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;

namespace Normora.Api.Extensions;

/// <summary>
/// Registers shared database and infrastructure services, delegating module-specific
/// data and external service configurations to each module's composition root.
/// </summary>
public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Hangfire Distributed Job Storage
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        // MinIO S3-compatible object storage client
        var minioOptions = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>() ?? new MinioOptions();
        services.AddMinio(configureClient => configureClient
            .WithEndpoint(minioOptions.Endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .WithSSL(false)
            .Build());

        // Distributed Cache (Redis with in-memory fallback for local dev / tests without Redis)
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "normora:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, Services.Caching.DistributedCacheService>();

        // Delegate to Module Composition Roots
        services.AddTenantsModule(configuration);
        services.AddDocumentsModule(configuration);
        services.AddConversationsModule(configuration);

        return services;
    }
}
