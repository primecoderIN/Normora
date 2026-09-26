import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { UserService } from '../services/user.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const userService = inject(UserService);
  const currentUser = userService.currentUser();

  if (currentUser && currentUser.memberships && currentUser.memberships.length > 0) {
    const activeTenantId = userService.activeTenantId();
    // SEC-13: If activeTenantId is null, fall back to the first membership but emit a
    // visible warning — silent fallback can cause requests to go to the wrong tenant.
    if (!activeTenantId) {
      console.warn('[TenantInterceptor] activeTenantId is null; falling back to memberships[0]. This may indicate a state desynchronization.');
    }
    const tenantId = activeTenantId || currentUser.memberships[0].tenantId;
    
    // Only intercept requests going to our API
    if (req.url.includes('/api/')) {
      const tenantReq = req.clone({
        headers: req.headers.set('X-Tenant-Id', tenantId)
      });
      return next(tenantReq);
    }
  }

  return next(req);
};
