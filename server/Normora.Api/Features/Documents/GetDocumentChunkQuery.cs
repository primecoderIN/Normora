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

        var chunk = await dbContext.DocumentChunks
            .Include(c => c.DocumentVersion)
                .ThenInclude(v => v.Document)
            .Where(c => c.Id == request.ChunkId)
            .FirstOrDefaultAsync(cancellationToken);

        if (chunk == null)
        {
            throw new BolaException(); // BOLA: Return 404 for missing or cross-tenant access
        }

        // Enforce tenant boundary
        if (chunk.DocumentVersion.Document.TenantId != tenantContext.TenantId.Value)
        {
            throw new BolaException();
        }

        // Apply department visibility rules
        var allowedDepartments = tenantContext.EffectiveDepartments;
        var hasAccess = chunk.DocumentVersion.Document.DepartmentIds == null || 
                        chunk.DocumentVersion.Document.DepartmentIds.Length == 0 || 
                        chunk.DocumentVersion.Document.DepartmentIds.Any(d => allowedDepartments.Contains(d));

        if (!hasAccess)
        {
            throw new BolaException();
        }

        return new DocumentChunkPreviewDto(
            chunk.Text,
            chunk.DocumentVersion.Document.FileName,
            chunk.Section,
            chunk.PageNumber
        );
    }
}
