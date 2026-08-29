import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map, take } from 'rxjs/operators';

export const roleGuard: CanActivateFn = (route) => {
  const oidcSecurityService = inject(OidcSecurityService);
  const router = inject(Router);

  // The required role is passed via the route definition's data property
  const requiredRole = route.data['role'] as string;

  return oidcSecurityService.getPayloadFromAccessToken().pipe(
    take(1),
    map((payload) => {
      if (!payload) {
        return router.createUrlTree(['/auth/login']);
      }

      // Check if the user has the required realm role in the Access Token
      const roles = payload.realm_access?.roles || [];
      
      if (roles.includes(requiredRole)) {
        return true;
      }

      // If they don't have the required role, bounce them to login
      console.warn(`Access denied. Missing role: ${requiredRole}`);
      return router.createUrlTree(['/auth/login']);
    })
  );
};
