using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Encapsulates contextual workspace information for a user, including personal sandbox ID and active organization details.
/// </summary>
public sealed record UserWorkspaceContextResult(
    Guid? PersonalTenantId,
    string ActiveTenantName,
    string? ActiveTenantSlug);

/// <summary>
/// Cross-module query to retrieve the user's personal tenant and active tenant metadata without leaking TenantsDbContext.
/// </summary>
public sealed record GetUserWorkspaceContextQuery(
    string ExternalUserId,
    Guid ActiveTenantId) : IRequest<UserWorkspaceContextResult>;
