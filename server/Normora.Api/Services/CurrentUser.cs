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
    public string KeycloakUserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    /// <inheritdoc />
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    /// <inheritdoc />
    public string? DisplayName => User?.FindFirst("name")?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value;

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
