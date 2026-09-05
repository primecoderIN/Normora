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
    public async Task ProcessAsync(Guid documentId, Guid tenantId)
    {
        // Hangfire has no HTTP tenant context, so bypass the request-scoped filter and
        // enforce ownership explicitly with both identifiers before touching the record.
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

        // Reprocessing only these states makes retries idempotent and prevents a late job
        // from overwriting a document that has already reached a later lifecycle state.
        if (document.Status is not (DocumentStatus.Uploaded or DocumentStatus.Failed))
        {
            logger.LogInformation(
                "Document processing job skipped because document {DocumentId} is already {Status}.",
                documentId,
                document.Status);
            return;
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            await context.SaveChangesAsync();
            await PublishStatusAsync(document);

            await using var documentStream = await storageService.DownloadDocumentAsync(document.MinioObjectName);
            document.ExtractedText = await textExtractor.ExtractAsync(documentStream, document.FileName);

            await context.DocumentChunks
                // A retry replaces the complete chunk set so partial previous work cannot duplicate results.
                .IgnoreQueryFilters()
                .Where(chunk => chunk.DocumentId == document.Id && chunk.TenantId == document.TenantId)
                .ExecuteDeleteAsync();

            var chunks = DocumentChunker.Split(document.ExtractedText);
            var documentChunks = chunks.Select((content, index) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                TenantId = document.TenantId,
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
            await context.SaveChangesAsync();

            // Ready is published only after all configured ingestion stages have completed.
            document.Status = DocumentStatus.Ready;
            await context.SaveChangesAsync();
            await PublishStatusAsync(document);

            logger.LogInformation(
                "Document {DocumentId} was extracted successfully for tenant {TenantId}.",
                documentId,
                tenantId);
        }
        catch (Exception exception)
        {
            document.Status = DocumentStatus.Failed;
            await context.SaveChangesAsync();
            await PublishStatusAsync(document);
            logger.LogError(exception, "Document {DocumentId} failed processing for tenant {TenantId}.", documentId, tenantId);
            throw;
        }
    }

    private Task PublishStatusAsync(Document document)
    {
        return hubContext.Clients.Group(DocumentHub.GroupName(document.TenantId))
            .SendAsync("DocumentStatusChanged", new DocumentStatusChanged(
                document.Id,
                document.TenantId,
                document.FileName,
                document.Status.ToString()));
    }
}