using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;
using Normora.Shared.Constants;

namespace Normora.Api.Features.Documents;

public record GetDocumentChunkQuery(Guid ChunkId) : IRequest<DocumentChunkPreviewDto>;

public record DocumentChunkPreviewDto(string Text, string DocumentName, string? Section, int? PageNumber);

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

        if (result == null)
        {
            throw new BolaException(); // BOLA: Return 403/404 for missing, cross-tenant access, or restricted department access
        }

        return new DocumentChunkPreviewDto(
            result.chunk.Content,
            result.document.FileName,
            null,
            null
        );
    }
}
