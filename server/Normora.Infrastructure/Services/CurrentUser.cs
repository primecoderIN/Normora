using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string KeycloakUserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? DisplayName => User?.FindFirst("name")?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
