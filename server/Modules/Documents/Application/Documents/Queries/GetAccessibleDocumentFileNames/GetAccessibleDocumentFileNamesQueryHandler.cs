using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Shared;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;

namespace Normora.Modules.Documents.Application.Documents.Queries;

/// <summary>
/// Cross-module query handler that resolves the list of accessible document file names for a user
/// based on their effective departmental scoping.
/// </summary>
/// <remarks>
/// Scoping Logic:
/// - Handled exclusively within the Documents module boundaries.
/// - Ingests the user's effective department IDs resolved by Tenants module.
/// - Matches Company-Wide documents (no department tags) plus documents tagged with matching departments.
/// - Used by Conversations auto-title and suggestion generators to prime prompt context.
/// </remarks>
public class GetAccessibleDocumentFileNamesQueryHandler(DocumentsDbContext context)
    : IRequestHandler<GetAccessibleDocumentFileNamesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(GetAccessibleDocumentFileNamesQuery request, CancellationToken cancellationToken)
    {
        // Global query filter on DocumentsDbContext automatically enforces TenantId and DeletedAt == null
        var query = context.Documents
            .AsNoTracking()
            .Where(d => d.Versions.Any(v => v.IsActive && v.Status == DocumentStatus.Ready));

        if (request.EffectiveDepartmentIds.Count == 0)
        {
            // Company-wide documents only (no departmental restrictions)
            query = query.Where(d => !d.DocumentDepartments.Any());
        }
        else
        {
            // Company-wide OR accessible via the user's effective departments
            query = query.Where(d => !d.DocumentDepartments.Any() || d.DocumentDepartments.Any(dep => request.EffectiveDepartmentIds.Contains(dep.DepartmentId)));
        }

        return await query
            .Select(d => d.FileName)
            .Distinct()
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
    }
}
