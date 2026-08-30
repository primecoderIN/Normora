import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { UserService } from '../services/user.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const userService = inject(UserService);
  const currentUser = userService.currentUser();

  if (currentUser && currentUser.memberships && currentUser.memberships.length > 0) {
    // For MVP, if the user belongs to a tenant, we inject their first tenant's ID
    // In the future, this can be dynamic based on the currently active workspace in the UI state
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
