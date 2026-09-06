using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

/// <summary>
/// A query to retrieve all documents belonging to the currently active tenant.
/// Notice that it does not take a TenantId as a parameter.
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
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(d => d.ToDto()).ToList();
    }
}
