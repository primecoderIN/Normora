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

        // Module-Private DbContext with pgvector Extension:
        // Configures Npgsql with pgvector vector operations enabled (.UseVector()).
        // Encapsulates the 'documents' database schema strictly within this module.
        services.AddDbContext<DocumentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        // S3-Compatible Binary Storage:
        // MinIO integration for binary document streaming and presigned URL downloads.
        services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();

        // Isolated Database Migrations:
        // Runs migrations specifically for the 'documents' PostgreSQL schema during host startup.
        services.AddScoped<IDatabaseMigrator, DocumentsDatabaseMigrator>();

        // Apache Tika Text Extraction Client:
        // Configured with a 5-minute timeout to accommodate large or OCR-heavy enterprise documents.
        var tikaOptions = configuration.GetSection(TikaOptions.SectionName).Get<TikaOptions>() ?? new TikaOptions();
        services.AddHttpClient<IDocumentTextExtractor, TikaDocumentTextExtractor>(client =>
        {
            client.BaseAddress = new Uri(tikaOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Google Gemini Embedding Client:
        // Configured with a 2-minute timeout for 768-dimensional text embedding generation.
        var geminiOptions = configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();
        services.AddHttpClient<ITextEmbeddingService, GeminiEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        // Internal Hybrid Retrieval Engine:
        // Encapsulated RRF service combining PostgreSQL full-text search with pgvector cosine distance.
        services.AddScoped<IRetrievalService, RetrievalService>();

        // Hangfire ETL Ingestion Worker:
        services.AddScoped<DocumentProcessingJob>();

        return services;
    }
}
