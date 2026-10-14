using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Cross-module contract to resolve the internal User Guid from a Keycloak user id claim.
/// </summary>
public record GetUserIdQuery(string KeycloakUserId) : IRequest<Guid?>;
