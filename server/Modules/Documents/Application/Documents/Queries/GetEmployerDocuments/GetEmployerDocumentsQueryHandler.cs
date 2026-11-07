using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Application.Documents.DTOs;
using Normora.Modules.Documents.Persistence;

namespace Normora.Modules.Documents.Application.Documents.Queries;

/// <summary>
/// Handles retrieving the documents.
/// Relies entirely on the DocumentsDbContext's Global Query Filter (powered by ITenantContext)
/// to automatically filter the results to only include documents owned by the active tenant.
/// </summary>
public sealed class GetEmployerDocumentsQueryHandler(DocumentsDbContext context) : IRequestHandler<GetEmployerDocumentsQuery, List<DocumentDto>>
{
    public async Task<List<DocumentDto>> Handle(GetEmployerDocumentsQuery request, CancellationToken cancellationToken)
    {
        // Global Multi-Tenant Query Filter & Department ACL Projection:
        // Relies on DocumentsDbContext global query filters to isolate documents strictly belonging to the active tenant
        // and filter out soft-deleted records (DeletedAt == null).
        // Eagerly includes DocumentDepartments (for department chip badges) and Versions (for revision history dropdowns).
        // Uses AsNoTracking to minimize heap allocation and avoid tracking overhead on read-heavy employer portal views.
        var documents = await context.Documents
            .Include(d => d.DocumentDepartments)
            .Include(d => d.Versions)
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        // Domain Model to DTO Projection:
        // Projects the active version's status, version number, and file size for UI consumption.
        return documents.Select(d => d.ToDto()).ToList();
    }
}

