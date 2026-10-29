using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Exceptions;

namespace Normora.Modules.Documents.Application.Mcp;

/// <summary>
/// Handles GetMcpDocumentContentQuery within Documents persistence context and applies BOLA/RBAC checks.
/// </summary>
public sealed class GetMcpDocumentContentQueryHandler(DocumentsDbContext documentsDb)
    : IRequestHandler<GetMcpDocumentContentQuery, McpDocumentContentResult?>
{
    public async Task<McpDocumentContentResult?> Handle(
        GetMcpDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        var document = await documentsDb.Documents
            .AsNoTracking()
            .Include(d => d.DocumentDepartments)
            .Include(d => d.Versions)
                .ThenInclude(v => v.Chunks)
            .FirstOrDefaultAsync(
                d => d.Id == request.DocumentId && d.TenantId == request.TenantId && d.DeletedAt == null,
                cancellationToken);

        if (document == null)
        {
            throw new BolaException();
        }

        var effectiveDepts = request.EffectiveDepartments.ToHashSet();
        if (!request.IsAdmin && document.DocumentDepartments.Any() && !document.DocumentDepartments.Any(dd => effectiveDepts.Contains(dd.DepartmentId)))
        {
            throw new BolaException();
        }

        var latestVersion = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        var extractedContent = latestVersion != null
            ? string.Join("\n\n", latestVersion.Chunks.OrderBy(c => c.ChunkIndex).Select(c => c.Content))
            : string.Empty;

        return new McpDocumentContentResult(
            document.Id,
            document.FileName,
            document.ContentType,
            extractedContent);
    }
}
