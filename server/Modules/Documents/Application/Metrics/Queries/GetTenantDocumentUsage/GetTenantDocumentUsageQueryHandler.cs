using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;

namespace Normora.Modules.Documents.Application.Metrics.Queries;

/// <summary>
/// Cross-module query handler that aggregates active document count and byte storage usage for a tenant.
/// Consumed by the Tenants module during subscription limit checks and quota enforcement.
/// </summary>
/// <remarks>
/// Quota Accounting:
/// - Uses .IgnoreQueryFilters() to bypass ambient tenant context and evaluate explicitly requested TenantId.
/// - Sums binary file size across all non-deleted documents to measure consumption against tier storage allowances.
/// </remarks>
public class GetTenantDocumentUsageQueryHandler(DocumentsDbContext context)
    : IRequestHandler<GetTenantDocumentUsageQuery, TenantDocumentUsageResult>
{
    public async Task<TenantDocumentUsageResult> Handle(GetTenantDocumentUsageQuery request, CancellationToken cancellationToken)
    {
        var docs = await context.Documents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => d.TenantId == request.TenantId && d.DeletedAt == null)
            .Select(d => new { d.Id, d.Size })
            .ToListAsync(cancellationToken);

        var docCount = docs.Count;
        var totalBytes = docs.Sum(d => d.Size);

        return new TenantDocumentUsageResult(docCount, totalBytes);
    }
}
