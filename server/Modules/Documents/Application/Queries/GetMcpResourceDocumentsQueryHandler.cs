using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;

namespace Normora.Modules.Documents.Application.Queries;

/// <summary>
/// Handles GetMcpResourceDocumentsQuery by filtering Documents within the Documents persistence context.
/// </summary>
public sealed class GetMcpResourceDocumentsQueryHandler(DocumentsDbContext documentsDb)
    : IRequestHandler<GetMcpResourceDocumentsQuery, IReadOnlyList<McpResourceItem>>
{
    public async Task<IReadOnlyList<McpResourceItem>> Handle(
        GetMcpResourceDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveDepts = request.EffectiveDepartments.ToHashSet();

        var query = documentsDb.Documents
            .AsNoTracking()
            .Include(d => d.DocumentDepartments)
            .Where(d => d.TenantId == request.TenantId && d.DeletedAt == null);

        if (!request.IsAdmin && effectiveDepts.Count > 0)
        {
            query = query.Where(d =>
                !d.DocumentDepartments.Any() ||
                d.DocumentDepartments.Any(dd => effectiveDepts.Contains(dd.DepartmentId)));
        }
        else if (!request.IsAdmin)
        {
            query = query.Where(d => !d.DocumentDepartments.Any());
        }

        var documents = await query
            .Select(d => new McpResourceItem(d.Id, d.FileName, d.ContentType))
            .ToListAsync(cancellationToken);

        return documents;
    }
}
