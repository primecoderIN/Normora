using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Normora.Shared;
using Normora.Shared.Interfaces;

namespace Normora.Api.Middleware;

/// <summary>
/// An authorization filter attribute that enforces tenant-level access.
/// It verifies that the user has a resolved tenant context and optionally checks if they have a specific role within that tenant.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireTenantAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. Retrieve the TenantContext that was populated by TenantResolutionMiddleware
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();

        // 2. Ensure the tenant is actually resolved (the user belongs to the tenant)
        if (!tenantContext.IsTenantResolved)
        {
            context.Result = new ObjectResult(ApiResponse.Failure("Tenant context is missing or invalid."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return Task.CompletedTask;
        }

        // 3. If specific roles were required (e.g. [RequireTenant("admin")]), enforce them strictly.
        //    BUG FIX: The old guard `&& !string.IsNullOrEmpty(tenantContext.TenantRole)` silently
        //    skipped the check when TenantRole was null, allowing any tenant member through.
        //    Now we deny if the role is absent OR doesn't match the required set.
        if (_roles.Length > 0)
        {
            if (string.IsNullOrEmpty(tenantContext.TenantRole) ||
                !_roles.Contains(tenantContext.TenantRole, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(ApiResponse.Failure("You do not have the required role in this workspace."))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
