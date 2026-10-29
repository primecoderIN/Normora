using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Documents.Application.Jobs;
using Normora.Modules.Documents.Application.Services;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Interfaces;
using Normora.Modules.Documents.Options;
using Normora.Shared.Options;

namespace Normora.Modules.Documents;

/// <summary>
/// Composition root for the Documents module.
/// Encapsulates document database context (with pgvector), MinIO object storage, Tika extraction,
/// Gemini vector embeddings, hybrid retrieval, and ingestion background jobs.
/// </summary>
public static class DocumentsModuleExtensions
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<DocumentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();
        services.AddScoped<IDatabaseMigrator, DocumentsDatabaseMigrator>();

        var tikaOptions = configuration.GetSection(TikaOptions.SectionName).Get<TikaOptions>() ?? new TikaOptions();
        services.AddHttpClient<IDocumentTextExtractor, TikaDocumentTextExtractor>(client =>
        {
            client.BaseAddress = new Uri(tikaOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        var geminiOptions = configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();
        services.AddHttpClient<ITextEmbeddingService, GeminiEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddScoped<IRetrievalService, RetrievalService>();
        services.AddScoped<DocumentProcessingJob>();

        return services;
    }
}
