using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

/// <summary>
/// A query to retrieve all documents belonging to the currently active tenant.
/// Notice that it does not take a TenantId as a parameter.
/// </summary>
public record GetEmployerDocumentsQuery() : IRequest<List<Document>>;

/// <summary>
/// Handles retrieving the documents.
/// Relies entirely on the DocumentsDbContext's Global Query Filter (powered by ITenantContext)
/// to automatically filter the results to only include documents owned by the active tenant.
/// </summary>
public sealed class GetEmployerDocumentsQueryHandler(DocumentsDbContext context) : IRequestHandler<GetEmployerDocumentsQuery, List<Document>>
{
    public async Task<List<Document>> Handle(GetEmployerDocumentsQuery request, CancellationToken cancellationToken)
    {
        // The Global Query Filter seamlessly appends `WHERE TenantId = @currentTenantId` to this query.
        return await context.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);
    }
}
