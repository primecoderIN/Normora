using MediatR;

namespace Normora.Shared.Contracts.Documents;

/// <summary>
/// Encapsulates a document resource descriptor exposed via the Model Context Protocol (MCP).
/// </summary>
public sealed record McpResourceItem(
    Guid DocumentId,
    string FileName,
    string ContentType);

/// <summary>
/// Cross-module query to list accessible document resources for the MCP server based on RBAC and department scope.
/// </summary>
public sealed record GetMcpResourceDocumentsQuery(
    Guid TenantId,
    bool IsAdmin,
    IReadOnlyCollection<Guid> EffectiveDepartments) : IRequest<IReadOnlyList<McpResourceItem>>;
