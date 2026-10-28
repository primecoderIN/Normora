using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Cross-module query to resolve a user's display name or email from their external identity subject identifier.
/// </summary>
public record GetUserDisplayNameQuery(string ExternalUserId) : IRequest<string?>
{
    public string KeycloakUserId => ExternalUserId;
}
