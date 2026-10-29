using MediatR;

namespace Normora.Shared.Contracts.Documents;

/// <summary>
/// Encapsulates extracted text and metadata for reading a document resource via MCP.
/// </summary>
public sealed record McpDocumentContentResult(
    Guid DocumentId,
    string FileName,
    string ContentType,
    string TextContent);

/// <summary>
/// Cross-module query to retrieve full document text for MCP resources/read, enforcing tenant boundary and department RBAC.
/// </summary>
public sealed record GetMcpDocumentContentQuery(
    Guid DocumentId,
    Guid TenantId,
    bool IsAdmin,
    IReadOnlyCollection<Guid> EffectiveDepartments) : IRequest<McpDocumentContentResult?>;
