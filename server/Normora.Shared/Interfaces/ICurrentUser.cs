namespace Normora.Shared.Interfaces;

public interface ICurrentUser
{
    string KeycloakUserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
}
