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
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var totalDocuments = await documentsDb.Documents.CountAsync(cancellationToken);
        var documentsThisMonth = await documentsDb.Documents
            .Where(d => d.UploadedAt >= thirtyDaysAgo)
            .CountAsync(cancellationToken);

        var recentDocs = await documentsDb.Documents
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
            .Select(d => d.FileName)
            .ToListAsync(cancellationToken);

        var failedDocs = await documentsDb.DocumentVersions
            .CountAsync(v => v.Status == DocumentStatus.Failed && v.IsActive, cancellationToken);
        var processingDocs = await documentsDb.DocumentVersions
            .CountAsync(v => v.Status == DocumentStatus.Processing && v.IsActive, cancellationToken);

        return new DocumentDashboardMetricsResult(
            totalDocuments,
            documentsThisMonth,
            recentItems,
            fileNames,
            failedDocs,
            processingDocs);
    }
}
