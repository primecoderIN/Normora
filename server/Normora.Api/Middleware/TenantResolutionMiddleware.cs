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
        // 1. We only resolve tenants for authenticated users.
        if (currentUser.IsAuthenticated)
        {
            // 2. Look for the X-Tenant-Id header sent by the client.
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
            {
                if (Guid.TryParse(tenantIdValues.FirstOrDefault(), out var tenantId))
                {
                    // Resolve active tenant membership and aggregate direct + user group department access via service boundary
                    var resolvedAccess = await tenantResolutionService.ResolveTenantAccessAsync(
                        tenantId,
                        currentUser.ExternalUserId,
                        context.RequestAborted);

                    if (resolvedAccess != null)
                    {
                        // Initialize ITenantContext so down-stream queries and [RequireTenant] filters enforce tenant isolation
                        tenantContext.SetContext(tenantId, resolvedAccess.Role, resolvedAccess.EffectiveDepartments);
                    }
                }
            }

            // Missing, malformed, or unauthorized tenant headers deliberately leave the
            // context unresolved; RequireTenant then rejects protected endpoints.
        }

        await next(context);
    }
}
