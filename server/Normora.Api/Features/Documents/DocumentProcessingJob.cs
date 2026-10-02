using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Normora.Api.Hubs;
using Normora.Modules.Documents.Persistence;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Background boundary for document ingestion. Text extraction and indexing are added in later slices.
/// </summary>
public sealed class DocumentProcessingJob(
    DocumentsDbContext context,
    IDocumentStorageService storageService,
    IDocumentTextExtractor textExtractor,
    ITextEmbeddingService embeddingService,
    IHubContext<DocumentHub> hubContext,
    ILogger<DocumentProcessingJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessAsync(Guid documentVersionId, Guid tenantId)
    {
        // Hangfire has no HTTP tenant context, so bypass the request-scoped filter and
        // enforce ownership explicitly with both identifiers before touching the record.
        var version = await context.DocumentVersions
            .Include(v => v.Document)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(v =>
                v.Id == documentVersionId && v.TenantId == tenantId);

        if (version is null || version.Document is null)
        {
            logger.LogWarning(
                "Document processing job skipped because document version {DocumentVersionId} was not found for tenant {TenantId}.",
                documentVersionId,
                tenantId);
            return;
        }

        // Reprocessing only these states makes retries idempotent and prevents a late job
        // from overwriting a document that has already reached a later lifecycle state.
        if (version.Status is not (DocumentStatus.Uploaded or DocumentStatus.Failed))
        {
            logger.LogInformation(
                "Document processing job skipped because document version {DocumentVersionId} is already {Status}.",
                documentVersionId,
                version.Status);
            return;
        }

        try
        {
            version.Status = DocumentStatus.Processing;
            await context.SaveChangesAsync();
            await PublishStatusAsync(version.Document, version.Status);

            await using var documentStream = await storageService.DownloadDocumentAsync(version.MinioObjectName);
            version.ExtractedText = await textExtractor.ExtractAsync(documentStream, version.Document.FileName);

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await context.DocumentChunks
                    // A retry replaces the complete chunk set so partial previous work cannot duplicate results.
                    .IgnoreQueryFilters()
                    .Where(chunk => chunk.DocumentVersionId == version.Id && chunk.TenantId == version.TenantId)
                    .ExecuteDeleteAsync();

            var chunks = DocumentChunker.Split(version.ExtractedText);
            var documentChunks = chunks.Select((content, index) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentVersionId = version.Id,
                TenantId = version.TenantId,
                ChunkIndex = index,
                Content = content
            }).ToList();

            if (embeddingService.IsConfigured)
            {
                // These chunks are new and still tracked in memory, so embed this list
                // directly instead of querying PostgreSQL before SaveChanges persists it.
                foreach (var chunk in documentChunks)
                {
                    chunk.Embedding = new Pgvector.Vector(
                        await embeddingService.CreateEmbeddingAsync(chunk.Content));
                }
            }

                context.DocumentChunks.AddRange(documentChunks);

                // Ready is published only after all configured ingestion stages have completed.
                version.Status = DocumentStatus.Ready;
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await PublishStatusAsync(version.Document, version.Status);

            logger.LogInformation(
                "Document version {DocumentVersionId} was extracted successfully for tenant {TenantId}.",
                documentVersionId,
                tenantId);
        }
        catch (Exception exception)
        {
            version.Status = DocumentStatus.Failed;
            await context.SaveChangesAsync();
            await PublishStatusAsync(version.Document, version.Status);
            logger.LogError(exception, "Document version {DocumentVersionId} failed processing for tenant {TenantId}.", documentVersionId, tenantId);
            throw;
        }
    }

    private Task PublishStatusAsync(Document document, DocumentStatus status)
    {
        return hubContext.Clients.Group(DocumentHub.GroupName(document.TenantId))
            .SendAsync("DocumentStatusChanged", new DocumentStatusChanged(
                document.Id,
                document.TenantId,
                document.FileName,
                status.ToString()));
    }
}