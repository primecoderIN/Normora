using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Chunks;

/// <summary>
/// Query to retrieve a specific text chunk for citation previews, enforcing tenant isolation and department RBAC.
/// </summary>
public record GetDocumentChunkQuery(Guid ChunkId) : IRequest<DocumentChunkPreviewDto>;

/// <summary>
/// Preview representation of a document chunk.
/// </summary>
public record DocumentChunkPreviewDto(string Text, string DocumentName, string? Section, int? PageNumber);

/// <summary>
/// Handles chunk retrieval ensuring the calling user has access to the owning department.
/// </summary>
public class GetDocumentChunkQueryHandler(DocumentsDbContext dbContext, ITenantContext tenantContext) : IRequestHandler<GetDocumentChunkQuery, DocumentChunkPreviewDto>
{
    public async Task<DocumentChunkPreviewDto> Handle(GetDocumentChunkQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException(ApiMessages.TenantContextMissing);
        }

        var allowedDepartments = tenantContext.EffectiveDepartments;
        var tenantId = tenantContext.TenantId.Value;

        // Verify chunk exists, belongs to caller's tenant, and satisfies department restrictions
        var result = await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => c.Id == request.ChunkId && c.TenantId == tenantId)
            .Join(
                dbContext.DocumentVersions.AsNoTracking(),
                chunk => chunk.DocumentVersionId,
                version => version.Id,
                (chunk, version) => new { chunk, version }
            )
            .Join(
                dbContext.Documents.AsNoTracking().Where(d =>
                    !d.DocumentDepartments.Any() ||
                    d.DocumentDepartments.Any(dd => allowedDepartments.Contains(dd.DepartmentId))),
                cv => cv.version.DocumentId,
                document => document.Id,
                (cv, document) => new { cv.chunk, document }
            )
            .FirstOrDefaultAsync(cancellationToken);

        // Protect against BOLA: throw BolaException if chunk does not exist or user lacks department permission
        if (result == null)
        {
            throw new BolaException();
        }

        return new DocumentChunkPreviewDto(
            result.chunk.Content,
            result.document.FileName,
            null,
            null
        );
    }
}
