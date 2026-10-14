using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Verifies whether the tenant has sufficient document quota and storage space to accept an upload.
/// Throws PlanLimitExceededException if quota is exceeded.
/// </summary>
public record CheckTenantUploadLimitQuery(Guid TenantId, long IncomingFileSizeBytes, bool IsNewDocument) : IRequest<bool>;
