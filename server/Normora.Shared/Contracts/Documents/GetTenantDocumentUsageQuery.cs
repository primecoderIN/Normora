using MediatR;

namespace Normora.Shared.Contracts.Documents;

/// <summary>
/// Result container for tenant document and storage metrics.
/// </summary>
public record TenantDocumentUsageResult(int DocumentCount, long TotalStorageBytes);

/// <summary>
/// Cross-module query to retrieve document count and storage bytes for a tenant.
/// </summary>
public record GetTenantDocumentUsageQuery(Guid TenantId) : IRequest<TenantDocumentUsageResult>;
