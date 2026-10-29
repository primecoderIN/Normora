using MediatR;

namespace Normora.Shared.Contracts.Documents;

/// <summary>
/// Encapsulates metadata for a recently uploaded document displayed on the dashboard.
/// </summary>
public sealed record DashboardRecentDocumentItem(
    string FileName,
    IReadOnlyList<Guid> DepartmentIds,
    string Status,
    string Type,
    DateTime UploadedAt);

/// <summary>
/// Encapsulates document statistics, recent uploads, file names, and ingestion health for the dashboard.
/// </summary>
public sealed record DocumentDashboardMetricsResult(
    int TotalDocuments,
    int DocumentsTrend,
    IReadOnlyList<DashboardRecentDocumentItem> RecentDocuments,
    IReadOnlyList<string> FileNames,
    int FailedDocsCount,
    int ProcessingDocsCount);

/// <summary>
/// Cross-module query to retrieve document metrics and recent uploads without leaking DocumentsDbContext.
/// </summary>
public sealed record GetDocumentDashboardMetricsQuery(Guid TenantId) : IRequest<DocumentDashboardMetricsResult>;
