namespace Normora.Shared.Interfaces;

/// <summary>
/// Encapsulates contextual identity and tenancy metadata passed to AI tools during execution.
/// </summary>
/// <param name="TenantId">The active workspace tenant identifier.</param>
/// <param name="TenantName">The friendly display name of the workspace.</param>
/// <param name="UserId">The Keycloak user subject identifier.</param>
/// <param name="UserEmail">The email address of the executing user.</param>
/// <param name="UserDisplayName">The display name of the executing user.</param>
/// <param name="PersonalTenantId">The user's personal tenant identifier, if any.</param>
public sealed record ToolExecutionContext(
    Guid TenantId,
    string TenantName,
    string? UserId,
    string? UserEmail,
    string? UserDisplayName,
    Guid? PersonalTenantId);
