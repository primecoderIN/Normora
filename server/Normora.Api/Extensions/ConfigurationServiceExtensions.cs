using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Shared.Options;

namespace Normora.Api.Extensions;

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
            
        // Apache Tika document text extraction service
        services.AddOptions<TikaOptions>()
            .Bind(configuration.GetSection(TikaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        // Stripe billing and subscription options
        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.SectionName));

        // SMTP and transactional email settings
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        return services;
    }
}

