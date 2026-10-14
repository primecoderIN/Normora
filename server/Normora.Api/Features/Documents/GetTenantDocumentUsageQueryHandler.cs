using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Cross-module query handler that aggregates active document count and byte storage usage for a tenant.
/// </summary>
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
