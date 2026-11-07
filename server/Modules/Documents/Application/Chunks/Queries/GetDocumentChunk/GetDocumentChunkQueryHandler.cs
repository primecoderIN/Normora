using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Application.Chunks.DTOs;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Chunks.Queries;

/// <summary>
/// Handles chunk retrieval ensuring the calling user has access to the owning department.
/// </summary>
public class GetDocumentChunkQueryHandler(DocumentsDbContext dbContext, ITenantContext tenantContext) : IRequestHandler<GetDocumentChunkQuery, DocumentChunkPreviewDto>
{
    public async Task<DocumentChunkPreviewDto> Handle(GetDocumentChunkQuery request, CancellationToken cancellationToken)
    {
        // Multi-Tenant Ambient Context Verification:
        // Validates that the request has an active tenant workspace context.
        if (!tenantContext.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException(ApiMessages.TenantContextMissing);
        }

        var isAdmin = string.Equals(tenantContext.TenantRole, TenantRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var allowedDepartments = tenantContext.EffectiveDepartments;
        var tenantId = tenantContext.TenantId.Value;

        // Citation Drawer ACL & BOLA Verification:
        // When a user clicks on an inline citation [1] in the chat response, the frontend requests
        // the underlying raw chunk text to preview the source context.
        // We ensure that:
        // 1. The chunk belongs to the ambient TenantId.
        // 2. The document is accessible via the user's effective department permissions.
        // If either check fails, we throw BolaException (mapped to 404) to avoid leaking the existence of restricted documents.
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
                    isAdmin ||
                    !d.DocumentDepartments.Any() ||
                    d.DocumentDepartments.Any(dd => allowedDepartments.Contains(dd.DepartmentId))),
                cv => cv.version.DocumentId,
                document => document.Id,
                (cv, document) => new { cv.chunk, document }
            )
            .FirstOrDefaultAsync(cancellationToken);

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

