using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Documents.Application.Jobs;
using Normora.Modules.Documents.Application.Services;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents;

/// <summary>
/// Composition root for the Documents module.
/// Encapsulates the document database context (with pgvector), hybrid retrieval, and ingestion background jobs.
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

        // Isolated Database Migrations:
        // Applies migrations for the 'documents' PostgreSQL schema during the one-off migration process.
        services.AddScoped<IDatabaseMigrator, DocumentsDatabaseMigrator>();

        // Internal Hybrid Retrieval Engine:
        // Encapsulated RRF service combining PostgreSQL full-text search with pgvector cosine distance.
        services.AddScoped<IRetrievalService, RetrievalService>();

        // Background document ingestion handler:
        services.AddScoped<DocumentProcessingJob>();

        return services;
    }
}
