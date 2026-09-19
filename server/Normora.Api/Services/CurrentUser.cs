using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Normora.Shared.Interfaces;

namespace Normora.Api.Services;

/// <summary>
/// A scoped service that extracts the current user's identity from the HTTP Context.
/// This acts as an adapter over ASP.NET Core's ClaimsPrincipal, decoupling our core modules from HTTP specifics.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    // When DefaultMapInboundClaims = false, Keycloak's raw 'sub' claim is NOT mapped
    // to the long Microsoft ClaimTypes.NameIdentifier URI. We must use the raw name.
    public string KeycloakUserId => User?.FindFirst("sub")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    /// <inheritdoc />
    // Same as above: raw 'email' claim instead of the long Microsoft URI.
    public string? Email => User?.FindFirst("email")?.Value ?? User?.FindFirst(ClaimTypes.Email)?.Value;

    /// <inheritdoc />
    public string? DisplayName => User?.FindFirst("name")?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
