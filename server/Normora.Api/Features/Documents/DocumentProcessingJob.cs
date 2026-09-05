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
    IHubContext<DocumentHub> hubContext,
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
                .IgnoreQueryFilters()
                .Where(chunk => chunk.DocumentId == document.Id && chunk.TenantId == document.TenantId)
                .ExecuteDeleteAsync();

            var chunks = DocumentChunker.Split(document.ExtractedText);
            context.DocumentChunks.AddRange(chunks.Select((content, index) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                TenantId = document.TenantId,
                ChunkIndex = index,
                Content = content
            }));

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