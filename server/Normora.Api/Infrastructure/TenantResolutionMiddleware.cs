using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Api.Infrastructure;

/// <summary>
/// Middleware responsible for extracting the active tenant from the request and validating the user's access.
/// This establishes the Tenant Context for the remainder of the HTTP request pipeline.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to resolve the tenant based on the X-Tenant-Id header and current user identity.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="tenantContext">The tenant context to be populated.</param>
    /// <param name="currentUser">The identity of the currently authenticated user.</param>
    /// <param name="tenantsDbContext">The database context for tenant-related information.</param>
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ICurrentUser currentUser, TenantsDbContext tenantsDbContext)
    {
        // 1. We only resolve tenants for authenticated users.
        if (currentUser.IsAuthenticated)
        {
            // 2. Look for the X-Tenant-Id header sent by the client.
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
            {
                if (Guid.TryParse(tenantIdValues.FirstOrDefault(), out var tenantId))
                {
                    // TENANT-12: Resolve user's tenant membership
                    // 3. Verify that the authenticated user is actually a member of this tenant.
                    var membership = await tenantsDbContext.TenantMemberships
                        .Include(m => m.User)
                        .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.User.KeycloakUserId == currentUser.KeycloakUserId);

                    if (membership != null)
                    {
                        // 4. If valid, initialize the ITenantContext so down-stream services (like AppDbContext) 
                        //    and authorization filters ([RequireTenant]) can enforce isolation.
                        tenantContext.SetContext(tenantId, membership.Role.ToString());
                    }
                }
            }
        }

        await _next(context);
    }
}
