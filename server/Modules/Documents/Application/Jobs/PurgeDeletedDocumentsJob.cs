using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Jobs;

/// <summary>
/// A nightly Hangfire background job that permanently deletes documents
/// (and their physical MinIO chunks) that were soft-deleted beyond the grace period.
/// </summary>
public sealed class PurgeDeletedDocumentsJob(
    DocumentsDbContext context,
    IDocumentStorageService storageService,
    ILogger<PurgeDeletedDocumentsJob> logger)
{
    private const int GracePeriodDays = 30;
    private const int BatchSize = 100;

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-GracePeriodDays);

        int purgedCount = 0;
        Guid? lastDocumentId = null;
        int failedCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var documentsToPurge = await context.Documents
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(d => d.Versions)
                .Where(d => d.DeletedAt != null &&
                            d.DeletedAt < cutoffDate &&
                            (!lastDocumentId.HasValue || d.Id.CompareTo(lastDocumentId.Value) > 0))
                .OrderBy(d => d.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (documentsToPurge.Count == 0)
            {
                break;
            }

            lastDocumentId = documentsToPurge[^1].Id;
            var deletedDocuments = new List<Document>(documentsToPurge.Count);
            foreach (var document in documentsToPurge)
            {
                try
                {
                    // Object deletion is idempotent, so retries recover if the database commit later fails.
                    foreach (var version in document.Versions)
                    {
                        if (!string.IsNullOrWhiteSpace(version.StorageObjectName))
                        {
                            await storageService.DeleteDocumentAsync(version.StorageObjectName);
                        }
                    }

                    deletedDocuments.Add(document);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogError(ex, "Failed to purge document {DocumentId} (Tenant: {TenantId}).", document.Id, document.TenantId);
                }
            }

            if (deletedDocuments.Count > 0)
            {
                // Due to cascade rules, this also removes versions, chunks, and department assignments.
                context.Documents.RemoveRange(deletedDocuments);
                await context.SaveChangesAsync(cancellationToken);
                purgedCount += deletedDocuments.Count;
                context.ChangeTracker.Clear();
            }
        }

        if (failedCount > 0)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.DocumentPurgeIncomplete);
        }

        logger.LogInformation("PurgeDeletedDocumentsJob: Purged {Count} documents.", purgedCount);
    }
}
