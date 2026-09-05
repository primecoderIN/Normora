using Hangfire;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Background boundary for document ingestion. Text extraction and indexing are added in later slices.
/// </summary>
public sealed class DocumentProcessingJob(
    DocumentsDbContext context,
    ILogger<DocumentProcessingJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessAsync(Guid documentId, Guid tenantId)
    {
        var document = await context.Documents
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(document =>
                document.Id == documentId && document.TenantId == tenantId);

        if (document is null)
        {
            logger.LogWarning(
                "Document processing job skipped because document {DocumentId} was not found for tenant {TenantId}.",
                documentId,
                tenantId);
            return;
        }

        if (document.Status != DocumentStatus.Uploaded)
        {
            logger.LogInformation(
                "Document processing job skipped because document {DocumentId} is already {Status}.",
                documentId,
                document.Status);
            return;
        }

        document.Status = DocumentStatus.Processing;
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Document {DocumentId} is now processing for tenant {TenantId}. Text extraction is the next ingestion step.",
            documentId,
            tenantId);
    }
}