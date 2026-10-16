using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using Normora.Modules.Tenants.Persistence;
using Normora.Modules.Documents.Persistence;
using Normora.Api.Features.Documents;
using Normora.Api.Features.Ask;
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
            options.UseNpgsql(connectionString)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddDbContext<DocumentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector())
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddDbContext<Normora.Modules.Conversations.Persistence.ConversationsDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        // Configure MinIO S3-compatible storage
        var minioOptions = configuration.GetSection(Normora.Shared.Options.MinioOptions.SectionName).Get<Normora.Shared.Options.MinioOptions>() ?? new Normora.Shared.Options.MinioOptions();

        services.AddMinio(configureClient => configureClient
            .WithEndpoint(minioOptions.Endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .WithSSL(false)
            .Build());

        services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();
        services.AddScoped<IBrandingStorageService, MinioBrandingStorageService>();
        
        var tikaOptions = configuration.GetSection(Normora.Shared.Options.TikaOptions.SectionName).Get<Normora.Shared.Options.TikaOptions>() ?? new Normora.Shared.Options.TikaOptions();
        services.AddHttpClient<IDocumentTextExtractor, TikaDocumentTextExtractor>(client =>
        {
            client.BaseAddress = new Uri(tikaOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        var geminiOptions = configuration.GetSection(Normora.Shared.Options.GeminiOptions.SectionName).Get<Normora.Shared.Options.GeminiOptions>() ?? new Normora.Shared.Options.GeminiOptions();
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
        
        services.AddHttpClient<Normora.Modules.Conversations.Application.Services.IQueryRewriterService, Normora.Modules.Conversations.Infrastructure.Llm.GeminiQueryRewriterService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        // Suggested Questions (Phase 24): lightweight Gemini generation call, no embedding pipeline.
        services.AddHttpClient<Normora.Modules.Conversations.Application.Services.ISuggestionsService, Normora.Modules.Conversations.Infrastructure.Llm.GeminiSuggestionsService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(30); // Shorter timeout — this is a UX-enhancing call, not critical path
        });

        return services;
    }
}
