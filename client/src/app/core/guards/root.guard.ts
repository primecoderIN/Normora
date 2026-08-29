import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map, take, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';

export const rootGuard: CanActivateFn = () => {
  const oidcSecurityService = inject(OidcSecurityService);
  const router = inject(Router);

  return oidcSecurityService.isAuthenticated$.pipe(
    take(1),
    switchMap(({ isAuthenticated }) => {
      if (!isAuthenticated) {
        return of(router.createUrlTree(['/auth/login']));
      }

      // IMPORTANT: realm_access.roles is in the ACCESS TOKEN payload, NOT in the
      // UserInfo endpoint response (userData$). Using userData$ here was a bug
      // that caused roles to always be empty, breaking role-based routing.
      return oidcSecurityService.getPayloadFromAccessToken().pipe(
        take(1),
        map((payload) => {
          const roles: string[] = payload?.realm_access?.roles ?? [];

          if (roles.includes('employer')) {
            return router.createUrlTree(['/employer/dashboard']);
          } else if (roles.includes('employee')) {
            return router.createUrlTree(['/employee/ask']);
          }

          // Authenticated but no app role — send to login with a clear error
          console.error(
            'rootGuard: user has no employer/employee role.',
            'Check Keycloak role mappers.',
            payload
          );
          return router.createUrlTree(['/auth/login']);
        })
      );
    })
  );
};
