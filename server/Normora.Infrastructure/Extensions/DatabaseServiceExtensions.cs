using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hangfire;
using Hangfire.PostgreSql;
using Normora.Modules.Conversations;
using Normora.Modules.Documents;
using Normora.Modules.Tenants;
using Normora.Shared.Interfaces;
using Normora.Shared.Constants;
using Normora.Infrastructure.Options;

namespace Normora.Infrastructure.Extensions;

/// <summary>
/// Registers shared database and persistence services, delegating module-specific
/// data models and DbContexts to each module's composition root.
/// </summary>
public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool enableHangfireServer = false,
        bool migrationMode = false)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (!migrationMode)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
            if (enableHangfireServer)
            {
                services.AddHangfireServer();
            }

            // Distributed Cache (Redis with in-memory fallback for local dev / tests without Redis)
            var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
            {
                redisOptions.ConnectionString = configuration.GetConnectionString(RedisOptions.SectionName)
                    ?? configuration[RedisOptions.EnvironmentVariable];
            }

            if (!string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisOptions.ConnectionString;
                    options.InstanceName = redisOptions.InstanceName;
                });
            }
            else
            {
                if (!environment.IsDevelopment())
                {
                    throw new InvalidOperationException(ApiMessages.RedisRequired);
                }

                services.AddDistributedMemoryCache();
            }

            services.AddSingleton<ICacheService, Normora.Infrastructure.Services.Caching.DistributedCacheService>();
        }
        else
        {
            // In migration mode, provide in-memory cache fallback for any services requiring ICacheService
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheService, Normora.Infrastructure.Services.Caching.DistributedCacheService>();
            services.AddScoped<ITenantContext, Normora.Infrastructure.Services.TenantContext>();
        }

        // Delegate to Module Composition Roots
        services.AddTenantsModule(configuration);
        services.AddDocumentsModule(configuration);
        services.AddConversationsModule(configuration);

        return services;
    }
}
