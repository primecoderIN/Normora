using Normora.Shared.Interfaces;

namespace Normora.Api.Middleware;

/// <summary>
/// Middleware responsible for extracting the active tenant from the request and validating the user's access.
/// This establishes the Tenant Context for the remainder of the HTTP request pipeline.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware to resolve the tenant based on the X-Tenant-Id header and current user identity.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="tenantContext">The tenant context to be populated.</param>
    /// <param name="tenantResolutionService">The service for resolving tenant access boundaries.</param>
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        ITenantResolutionService tenantResolutionService)
    {
        // Tenant resolution only applies to authenticated security principals.
        if (currentUser.IsAuthenticated)
        {
            // Extract the active workspace ID supplied by the client interceptor.
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
            {
                if (Guid.TryParse(tenantIdValues.FirstOrDefault(), out var tenantId))
                {
                    // Query the cached tenant resolution service to validate membership and compute effective departmental clearances.
                    var resolvedAccess = await tenantResolutionService.ResolveTenantAccessAsync(
                        tenantId,
                        currentUser.ExternalUserId,
                        context.RequestAborted);

                    if (resolvedAccess != null)
                    {
                        // Bind resolved workspace scope to ITenantContext for downstream EF query filters and [RequireTenant] guards.
                        tenantContext.SetContext(tenantId, resolvedAccess.Role, resolvedAccess.EffectiveDepartments);
                    }
                }
            }

            // Unresolved or unauthorized tenant scopes remain unset; [RequireTenant] filters will reject downstream access.
        }

        await next(context);
    }
}
