using MediatR;
using Microsoft.EntityFrameworkCore;

using Normora.Modules.Documents.Persistence;

namespace Normora.Modules.Documents.Application.Documents;

/// <summary>
/// A query to retrieve all documents belonging to the currently active tenant.
/// </summary>
public record GetEmployerDocumentsQuery : IRequest<List<DocumentDto>>;

/// <summary>
/// Handles retrieving the documents.
/// Relies entirely on the DocumentsDbContext's Global Query Filter (powered by ITenantContext)
/// to automatically filter the results to only include documents owned by the active tenant.
/// </summary>
public sealed class GetEmployerDocumentsQueryHandler(DocumentsDbContext context) : IRequestHandler<GetEmployerDocumentsQuery, List<DocumentDto>>
{
    public async Task<List<DocumentDto>> Handle(GetEmployerDocumentsQuery request, CancellationToken cancellationToken)
    {
        var documents = await context.Documents
            .Include(d => d.DocumentDepartments)
            .Include(d => d.Versions)
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(d => d.ToDto()).ToList();
    }
}
