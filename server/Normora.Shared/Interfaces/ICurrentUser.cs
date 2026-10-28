namespace Normora.Shared.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user's identity claims, extracted from their JWT token.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The unique 'sub' identifier provided by the external identity provider.
    /// </summary>
    string ExternalUserId { get; }

    /// <summary>
    /// Backwards compatibility alias for ExternalUserId.
    /// </summary>
    string KeycloakUserId => ExternalUserId;

    /// <summary>
    /// The user's email address, sourced from the JWT claims.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The user's preferred display name, sourced from the JWT claims.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Indicates whether a valid user identity is present in the current HTTP context.
    /// </summary>
    bool IsAuthenticated { get; }
}
