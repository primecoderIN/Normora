using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Cross-module query to resolve a user's display name or email from their Keycloak identifier.
/// </summary>
public record GetUserDisplayNameQuery(string KeycloakUserId) : IRequest<string?>;
