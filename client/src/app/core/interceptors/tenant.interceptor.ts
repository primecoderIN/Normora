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

  if (currentUser && currentUser.memberships && currentUser.memberships.length > 0) {
    const activeTenantId = userService.activeTenantId();
    if (!activeTenantId) {
      console.warn('[TenantInterceptor] activeTenantId is unset; defaulting to first membership.');
    }
    const tenantId = activeTenantId || currentUser.memberships[0].tenantId;

    // Attach tenant header only to first-party API endpoints
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
