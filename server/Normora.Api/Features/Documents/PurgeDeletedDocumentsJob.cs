using Hangfire;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Api.Features.Documents;

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

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-GracePeriodDays);

        // We must IgnoreQueryFilters() because soft-deleted documents are globally filtered out.
        // Also note this is a tenant-agnostic job; it purges for ALL tenants at once.
        var documentsToPurge = await context.Documents
            .IgnoreQueryFilters()
            .Include(d => d.Versions)
            .Where(d => d.DeletedAt != null && d.DeletedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (documentsToPurge.Count == 0)
        {
            logger.LogInformation("PurgeDeletedDocumentsJob: No documents found beyond the {GracePeriod} day grace period.", GracePeriodDays);
            return;
        }

        int purgedCount = 0;

        foreach (var document in documentsToPurge)
        {
            try
            {
                // Delete physical objects from MinIO for all versions of this document
                foreach (var version in document.Versions)
                {
                    if (!string.IsNullOrWhiteSpace(version.MinioObjectName))
                    {
                        await storageService.DeleteDocumentAsync(version.MinioObjectName);
                    }
                }

                // Delete from Postgres. Due to CASCADE rules configured in DocumentsDbContext,
                // this also deletes DocumentVersions, DocumentChunks, and DocumentDepartments.
                context.Documents.Remove(document);
                purgedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to purge document {DocumentId} (Tenant: {TenantId}).", document.Id, document.TenantId);
                // Continue with other documents even if one fails
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("PurgeDeletedDocumentsJob: Successfully purged {Count} documents.", purgedCount);
    }
}
