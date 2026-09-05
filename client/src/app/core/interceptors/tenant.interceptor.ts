import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { UserService } from '../services/user.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const userService = inject(UserService);
  const currentUser = userService.currentUser();

  if (currentUser && currentUser.memberships && currentUser.memberships.length > 0) {
    // Until tenant switching exists, the first membership is the active workspace. This is
    // only a routing hint; the API independently verifies membership before resolving context.
    const tenantId = currentUser.memberships[0].tenantId;
    
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
