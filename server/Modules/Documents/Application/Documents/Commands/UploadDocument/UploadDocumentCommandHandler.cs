using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Application.Documents.DTOs;
using Normora.Modules.Documents.Application.Jobs;
using Normora.Modules.Documents.Domain;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Commands;

/// <summary>
/// Handles the execution of UploadDocumentCommand.
/// Responsible for streaming the file to MinIO storage and saving metadata to the database.
/// </summary>
public sealed class UploadDocumentCommandHandler(
    DocumentsDbContext context,
    IDocumentStorageService storageService,
    IBackgroundJobClient backgroundJobClient,
    IDocumentNotificationService notificationService,
    IMediator mediator,
    ICacheService cache) : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // Cross-Module Subscription Boundary Verification:
        // Before allocating object storage or database resources, query the Tenants module via MediatR
        // to enforce subscription tier quotas (e.g. max documents, max total storage bytes).
        // If quotas are exceeded, this throws PlanLimitExceededException (mapped to 402 Payment Required).
        await mediator.Send(new Normora.Shared.Contracts.Tenants.CheckTenantUploadLimitQuery(
            request.TenantId,
            request.File.Length,
            !request.DocumentId.HasValue), cancellationToken);

        // Object Store Binary Upload:
        // Stream the binary payload directly into MinIO/S3 using a tenant-isolated storage path prefix.
        // We persist the binary in object storage before inserting relational metadata into PostgreSQL.
        var objectName = await storageService.UploadDocumentAsync(request.File, request.TenantId.ToString());

        // Compensating Transaction Wrapper (Saga Pattern):
        // If relational database insertion fails (e.g. database timeout or constraint violation),
        // we execute a compensating rollback step to purge the orphaned binary from MinIO object storage.
        try
        {
            Document document;
            int nextVersionNumber = 1;

            if (request.DocumentId.HasValue)
            {
                // Document Versioning Workflow:
                // When request.DocumentId is provided, we append a new version to an existing document container.
                // We perform a BOLA check ensuring the document exists and belongs to the active tenant.
                var existingDoc = await context.Documents
                    .Include(d => d.Versions)
                    .SingleOrDefaultAsync(d => d.Id == request.DocumentId.Value && d.TenantId == request.TenantId, cancellationToken);

                if (existingDoc == null) throw new BolaException();
                document = existingDoc;

                if (document.Versions.Any())
                {
                    // Compute next sequential version number
                    nextVersionNumber = document.Versions.Max(v => v.VersionNumber) + 1;

                    // Invariant: Single Active Version for Retrieval
                    // Deactivate prior versions (IsActive = false).
                    // The hybrid retrieval pipeline (Full-Text + Vector Cosine) filters exclusively on IsActive == true
                    // to ensure conversational RAG never retrieves obsolete or superseded knowledge.
                    foreach (var v in document.Versions)
                    {
                        v.IsActive = false;
                    }
                }
            }
            else
            {
                // New Document Container Creation:
                // Initialize the root Document entity and attach ACL department bindings.
                // If department IDs are omitted, the document is accessible globally within the tenant.
                document = new Document
                {
                    Id = Guid.NewGuid(),
                    FileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    Size = request.File.Length,
                    UploadedAt = DateTime.UtcNow,
                    TenantId = request.TenantId,
                    DocumentDepartments = request.DepartmentIds?.Select(depId => new DocumentDepartment
                    {
                        DepartmentId = depId
                    }).ToList() ?? new List<DocumentDepartment>()
                };
                context.Documents.Add(document);
            }

            // Document Version Entity Instantiation:
            // Represents the immutable physical file revision with its storage object pointer and processing status.
            var version = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                TenantId = request.TenantId,
                VersionNumber = nextVersionNumber,
                StorageObjectName = objectName,
                Status = DocumentStatus.Uploaded,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            document.Versions.Add(version);
            context.DocumentVersions.Add(version);
            await context.SaveChangesAsync(cancellationToken);

            // Distributed Cache Invalidation:
            // Evict cached dashboard metrics so the employer management portal reflects accurate document counts and storage usage.
            await cache.RemoveAsync($"dashboard:{document.TenantId}", cancellationToken);

            // Real-Time SignalR Broadcast:
            // Notify connected frontend workspace clients that a new document has been uploaded and queued.
            await notificationService.SendDocumentStatusChangedAsync(
                document.TenantId,
                document.Id,
                document.FileName,
                version.Status.ToString(),
                cancellationToken);

            // Asynchronous ETL Pipeline Dispatch:
            // Enqueue DocumentProcessingJob in Hangfire to execute text extraction (Apache Tika),
            // sliding-window semantic chunking, and embedding generation (Gemini 768-dim) off the HTTP request thread.
            backgroundJobClient.Enqueue<DocumentProcessingJob>(job =>
                job.ProcessAsync(version.Id, document.TenantId));

            return document.ToDto();
        }
        catch
        {
            // Compensating Rollback:
            // Purge the uploaded binary from object storage to prevent orphaned disk leaks if DB transaction aborts.
            try { await storageService.DeleteDocumentAsync(objectName); } catch { /* intentionally swallowed */ }
            throw;
        }
    }
}

