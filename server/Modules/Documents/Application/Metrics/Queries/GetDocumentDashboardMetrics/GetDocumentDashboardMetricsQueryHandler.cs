using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Shared;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;

namespace Normora.Modules.Documents.Application.Metrics.Queries;

/// <summary>
/// Cross-module query handler calculating knowledge base volume, upload trends, and recent documents
/// to construct the unified Employer Analytics Dashboard.
/// </summary>
/// <remarks>
/// Metrics Pipeline:
/// - Aggregates total active documents and documents ingested in the last 30 days.
/// - Identifies failing document versions requiring employer remediation.
/// - Projects the 5 most recent uploads with departmental access tags and file extensions.
/// </remarks>
public sealed class GetDocumentDashboardMetricsQueryHandler(DocumentsDbContext documentsDb)
    : IRequestHandler<GetDocumentDashboardMetricsQuery, DocumentDashboardMetricsResult>
{
    public async Task<DocumentDashboardMetricsResult> Handle(
        GetDocumentDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return new DocumentDashboardMetricsResult(0, 0, Array.Empty<DashboardRecentDocumentItem>(), Array.Empty<string>(), 0, 0);
        }

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var totalDocuments = await documentsDb.Documents
            .Where(d => d.TenantId == request.TenantId)
            .CountAsync(cancellationToken);

        var documentsThisMonth = await documentsDb.Documents
            .Where(d => d.TenantId == request.TenantId && d.UploadedAt >= thirtyDaysAgo)
            .CountAsync(cancellationToken);

        var recentDocs = await documentsDb.Documents
            .Where(d => d.TenantId == request.TenantId)
            .Include(d => d.DocumentDepartments)
            .Include(d => d.Versions)
            .OrderByDescending(d => d.UploadedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentItems = recentDocs.Select(d => new DashboardRecentDocumentItem(
            d.FileName,
            d.DocumentDepartments.Select(dd => dd.DepartmentId).ToList(),
            d.Versions.FirstOrDefault(v => v.IsActive)?.Status.ToString() ?? "Unknown",
            System.IO.Path.GetExtension(d.FileName).TrimStart('.').ToUpperInvariant(),
            d.UploadedAt
        )).ToList();

        var fileNames = await documentsDb.Documents
            .Where(d => d.TenantId == request.TenantId)
            .Select(d => d.FileName)
            .ToListAsync(cancellationToken);

        var failedDocs = await documentsDb.DocumentVersions
            .Where(v => v.TenantId == request.TenantId && v.Status == DocumentStatus.Failed && v.IsActive)
            .CountAsync(cancellationToken);

        var processingDocs = await documentsDb.DocumentVersions
            .Where(v => v.TenantId == request.TenantId && v.Status == DocumentStatus.Processing && v.IsActive)
            .CountAsync(cancellationToken);

        return new DocumentDashboardMetricsResult(
            totalDocuments,
            documentsThisMonth,
            recentItems,
            fileNames,
            failedDocs,
            processingDocs);
    }
}
