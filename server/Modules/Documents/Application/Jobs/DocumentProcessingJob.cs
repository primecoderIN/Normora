using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Shared;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Jobs;

/// <summary>
/// Hangfire background job orchestrating document ingestion: text extraction (Tika), semantic chunking,
/// vector embedding generation (Gemini), chunk persistence, cache eviction, and real-time client notifications.
/// </summary>
public sealed class DocumentProcessingJob(
    DocumentsDbContext context,
    MediatR.IMediator mediator,
    IDocumentStorageService storageService,
    IDocumentTextExtractor textExtractor,
    ITextEmbeddingService embeddingService,
    IDocumentNotificationService notificationService,
    IEmailService emailService,
    ICacheService cache,
    ILogger<DocumentProcessingJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    // Processes an individual DocumentVersion, ensuring separate storage keys, extracted text, and vector chunks per revision.
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

            await using var documentStream = await storageService.DownloadDocumentAsync(version.StorageObjectName);
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

            // Evict cached dashboard metrics so processing status is updated
            await cache.RemoveAsync($"dashboard:{tenantId}");

            // Notify employers that the document is ready for queries
            await NotifyDocumentReadyAsync(version.Document, tenantId);
        }
        catch (Exception exception)
        {
            version.Status = DocumentStatus.Failed;
            await context.SaveChangesAsync();
            await PublishStatusAsync(version.Document, version.Status);
            logger.LogError(exception, "Document version {DocumentVersionId} failed processing for tenant {TenantId}.", documentVersionId, tenantId);

            // Evict cached dashboard metrics on failure
            await cache.RemoveAsync($"dashboard:{tenantId}");

            // Notify employers about the processing failure
            await NotifyDocumentProcessingFailedAsync(version.Document, tenantId, exception.Message);

            throw;
        }
    }

    private async Task NotifyDocumentProcessingFailedAsync(Document document, Guid tenantId, string errorMessage)
    {
        try
        {
            var adminRecipients = await mediator.Send(new Normora.Shared.Contracts.Tenants.GetTenantAdminRecipientsQuery(tenantId));
            var tenantName = adminRecipients.TenantName;
            var adminEmails = adminRecipients.AdminEmails;

            if (adminEmails.Count == 0)
            {
                logger.LogInformation("No employer email recipients found for tenant {TenantId}.", tenantId);
                return;
            }

            var html = Normora.Shared.Templates.EmailTemplates.DocumentProcessingFailedEmail(tenantName, document.FileName, errorMessage);
            var subject = $"[Action Required] Processing failed: {document.FileName}";

            foreach (var email in adminEmails)
            {
                await emailService.SendEmailAsync(email, subject, html);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch document failure notification emails for document {DocumentId}.", document.Id);
        }
    }

    private async Task NotifyDocumentReadyAsync(Document document, Guid tenantId)
    {
        try
        {
            var adminRecipients = await mediator.Send(new Normora.Shared.Contracts.Tenants.GetTenantAdminRecipientsQuery(tenantId));
            var tenantName = adminRecipients.TenantName;
            var employerEmails = adminRecipients.AdminEmails;

            var departments = await context.DocumentDepartments
                .Where(dd => dd.DocumentId == document.Id)
                .ToListAsync();

            string scopeDescription = departments.Count == 0 ? "Company-Wide" : $"{departments.Count} Department(s)";

            var html = Normora.Shared.Templates.EmailTemplates.DocumentReadyEmail(tenantName, document.FileName, scopeDescription);
            var subject = $"New document available: {document.FileName}";

            foreach (var email in employerEmails)
            {
                await emailService.SendEmailAsync(email, subject, html);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch document ready notification emails for document {DocumentId}.", document.Id);
        }
    }

    private Task PublishStatusAsync(Document document, DocumentStatus status)
    {
        return notificationService.SendDocumentStatusChangedAsync(
            document.TenantId,
            document.Id,
            document.FileName,
            status.ToString());
    }
}
