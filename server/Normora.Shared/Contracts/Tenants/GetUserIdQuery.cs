using MediatR;

namespace Normora.Shared.Contracts.Tenants;

/// <summary>
/// Cross-module contract to resolve the internal User Guid from an external user id (e.g. sub claim).
/// </summary>
public record GetUserIdQuery(string ExternalUserId) : IRequest<Guid?>
{
    public string KeycloakUserId => ExternalUserId;
}
