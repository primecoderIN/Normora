using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Shared.Authorization;

/// <summary>
/// An authorization filter attribute that enforces tenant-level access.
/// It verifies that the user has a resolved tenant context and optionally checks if they have a specific role within that tenant.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute(params string[] roles) : Attribute, IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. Retrieve the TenantContext that was populated by TenantResolutionMiddleware
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();

        // 2. Ensure the tenant is actually resolved (the user belongs to the tenant)
        if (!tenantContext.IsTenantResolved)
        {
            // BFLA Status Normalization:
            // If the user has a valid authentication session (User.Identity.IsAuthenticated == true),
            // but is attempting to access a workspace where they lack membership, return 403 Forbidden.
            // Returning 401 Unauthorized here would trigger the frontend HTTP interceptor to forcibly
            // navigate to /login and destroy the user's active Keycloak session.
            var statusCode = context.HttpContext.User.Identity?.IsAuthenticated == true
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;

            context.Result = new ObjectResult(ApiResponse.Failure(ApiMessages.TenantContextMissing))
            {
                StatusCode = statusCode
            };
            return Task.CompletedTask;
        }

        // 3. If specific roles were required (e.g. [RequireTenant("admin")]), enforce them strictly.
        if (roles.Length > 0)
        {
            if (string.IsNullOrEmpty(tenantContext.TenantRole) ||
                !roles.Contains(tenantContext.TenantRole, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(ApiResponse.Failure(ApiMessages.RoleForbid))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}

