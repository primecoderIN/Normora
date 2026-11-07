import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { UserService } from '../services/user.service';
import { environment } from '@env/environment';

/**
 * HTTP interceptor that attaches the `X-Tenant-Id` header to outgoing API and BFF requests,
 * ensuring backend middleware establishes the correct multi-tenant database context.
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const userService = inject(UserService);
  const currentUser = userService.currentUser();

  // Multi-tenancy header injection:
  // Evaluates whether the user is authenticated and possesses active tenant memberships.
  // The backend's TenantResolutionMiddleware reads 'X-Tenant-Id' to bind the ambient ITenantContext,
  // which EF Core uses to dynamically filter database queries by TenantId.
  if (currentUser && currentUser.memberships && currentUser.memberships.length > 0) {
    const activeTenantId = userService.activeTenantId();
    if (!activeTenantId) {
      // If the path-based router has not yet set the active workspace signal (e.g., during initial app boot),
      // default to the user's primary workspace to ensure core bootstrap queries do not fail with TenantContextMissing.
      console.warn('[TenantInterceptor] activeTenantId is unset; defaulting to first membership.');
    }
    const tenantId = activeTenantId || currentUser.memberships[0].tenantId;

    // Strict URL boundary check:
    // Only attach the 'X-Tenant-Id' header to internal API and BFF endpoints.
    // Never leak internal tenant GUIDs or trigger unnecessary CORS preflights on third-party URLs.
    const isApiRequest = req.url.startsWith('/api/') || 
                         req.url.startsWith('/bff/') || 
                         (Boolean(environment.apiUrl) && req.url.startsWith(environment.apiUrl));

    if (isApiRequest) {
      const tenantReq = req.clone({
        headers: req.headers.set('X-Tenant-Id', tenantId)
      });
      return next(tenantReq);
    }
  }

  return next(req);
};
