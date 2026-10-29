using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Encapsulates tenant name and email addresses of active tenant administrators.
/// </summary>
public sealed record TenantAdminRecipientsResult(
    string TenantName,
    IReadOnlyList<string> AdminEmails);

/// <summary>
/// Cross-module query to retrieve tenant name and administrator emails for transactional notifications without leaking TenantsDbContext.
/// </summary>
public sealed record GetTenantAdminRecipientsQuery(Guid TenantId) : IRequest<TenantAdminRecipientsResult>;
