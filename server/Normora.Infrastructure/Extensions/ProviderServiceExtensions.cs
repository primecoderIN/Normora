using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Minio;
using Normora.Infrastructure.Providers.DocumentExtraction;
using Normora.Infrastructure.Providers.Gemini;
using Normora.Infrastructure.Providers.Storage;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Documents.Application.Services;
using Normora.Modules.Documents.Persistence;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Options;

namespace Normora.Infrastructure.Extensions;

/// <summary>
/// Registers external third-party infrastructure providers (Object Storage, Document Extraction, and AI/LLM),
/// implementing domain contracts owned by business modules.
/// </summary>
public static class ProviderServiceExtensions
{
    public static IServiceCollection AddExternalProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.AddStorageProviders(configuration, environment);
        services.AddDocumentExtractionProviders(configuration);
        services.AddAiProviders(configuration);

        return services;
    }

    public static IServiceCollection AddStorageProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var minioOptions = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>() ?? new MinioOptions();
        if (environment != null && !environment.IsDevelopment() && !minioOptions.UseSsl)
        {
            throw new InvalidOperationException(ApiMessages.MinioTlsRequired);
        }

        services.AddMinio(configureClient => configureClient
            .WithEndpoint(minioOptions.Endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .WithSSL(minioOptions.UseSsl)
            .Build());

        services.AddScoped<IBrandingStorageService, MinioBrandingStorageService>();
        services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();

        return services;
    }

    public static IServiceCollection AddDocumentExtractionProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var geminiOptions = configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();
        services.AddHttpClient<IDocumentTextExtractor, GeminiDocumentTextExtractor>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        return services;
    }

    public static IServiceCollection AddAiProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var geminiOptions = configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();

        services.AddHttpClient<ITextEmbeddingService, GeminiEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<ITextGenerationService, GeminiTextGenerationService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<IQueryRewriterService, GeminiQueryRewriterService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<ISuggestionsService, GeminiSuggestionsService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}

