using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Tenants.Options;
using Normora.Shared.Options;
using Normora.Infrastructure.Options;
using Normora.Infrastructure.Providers.Gemini;
using Normora.Infrastructure.Providers.Storage;

namespace Normora.Infrastructure.Extensions;

/// <summary>
/// Registers strongly typed configuration options with startup validation.
/// </summary>
public static class ConfigurationServiceExtensions
{
    /// <summary>
    /// Binds configuration sections into typed options and validates them at startup.
    /// </summary>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core application settings
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        // Keycloak identity and OAuth settings
        services.AddOptions<KeycloakOptions>()
            .Bind(configuration.GetSection(KeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        // Gemini AI / Google Vertex AI options
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        // MinIO object storage options
        services.AddOptions<MinioOptions>()
            .Bind(configuration.GetSection(MinioOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        // PayU billing and subscription options
        services.AddOptions<PayUOptions>()
            .Bind(configuration.GetSection(PayUOptions.SectionName));

        // SMTP and transactional email settings
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        // Redis distributed caching and SignalR pub/sub backplane options
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .Configure(options =>
            {
                // Fallback to ConnectionStrings:Redis or REDIS_CONNECTION if ConnectionString is not explicitly specified
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    options.ConnectionString = configuration.GetConnectionString(RedisOptions.SectionName)
                        ?? configuration[RedisOptions.EnvironmentVariable];
                }
            });

        return services;
    }
}
