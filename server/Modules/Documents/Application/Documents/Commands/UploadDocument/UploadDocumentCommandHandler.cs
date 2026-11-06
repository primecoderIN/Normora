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
        // Enforce subscription plan limits before initiating binary storage upload
        await mediator.Send(new Normora.Shared.Contracts.Tenants.CheckTenantUploadLimitQuery(
            request.TenantId,
            request.File.Length,
            !request.DocumentId.HasValue), cancellationToken);

        // Persist the binary in object storage before database metadata is created.
        var objectName = await storageService.UploadDocumentAsync(request.File, request.TenantId.ToString());

        // Wrap subsequent operations in a compensating try/catch to delete the MinIO object if database persistence fails
        try
        {
            Document document;
            int nextVersionNumber = 1;

            if (request.DocumentId.HasValue)
            {
                // Append a new version to an existing document container
                var existingDoc = await context.Documents
                    .Include(d => d.Versions)
                    .SingleOrDefaultAsync(d => d.Id == request.DocumentId.Value && d.TenantId == request.TenantId, cancellationToken);

                if (existingDoc == null) throw new BolaException();
                document = existingDoc;

                if (document.Versions.Any())
                {
                    // Compute next sequential version number
                    nextVersionNumber = document.Versions.Max(v => v.VersionNumber) + 1;

                    // Deactivate prior versions so the retrieval pipeline only matches against the newest revision
                    foreach (var v in document.Versions)
                    {
                        v.IsActive = false;
                    }
                }
            }
            else
            {
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

            // Create the new active DocumentVersion record
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

            // Invalidate cached dashboard metrics for this tenant
            await cache.RemoveAsync($"dashboard:{document.TenantId}", cancellationToken);

            // Broadcast status change via notification service before scheduling background work
            await notificationService.SendDocumentStatusChangedAsync(
                document.TenantId,
                document.Id,
                document.FileName,
                version.Status.ToString(),
                cancellationToken);

            backgroundJobClient.Enqueue<DocumentProcessingJob>(job =>
                job.ProcessAsync(version.Id, document.TenantId));

            return document.ToDto();
        }
        catch
        {
            // Best-effort compensation: purge the uploaded binary from MinIO storage on failure
            try { await storageService.DeleteDocumentAsync(objectName); } catch { /* intentionally swallowed */ }
            throw;
        }
    }
}

