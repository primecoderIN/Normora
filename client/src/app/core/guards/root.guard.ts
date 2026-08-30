import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map, take, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { UserService } from '../services/user.service';

export const rootGuard: CanActivateFn = () => {
  const oidcSecurityService = inject(OidcSecurityService);
  const router = inject(Router);
  const userService = inject(UserService);

  return oidcSecurityService.isAuthenticated$.pipe(
    take(1),
    switchMap(({ isAuthenticated }) => {
      if (!isAuthenticated) {
        return of(router.createUrlTree(['/auth/login']));
      }

      // Fetch the current user profile from the backend
      return userService.getMe().pipe(
        map(response => {
          if (!response.success || !response.data) {
            // Backend issue or user not found at all
            return router.createUrlTree(['/auth/login']);
          }

          const memberships = response.data.memberships;

          // If the user has no memberships, they need to onboard or accept an invite
          if (memberships.length === 0) {
            return router.createUrlTree(['/onboarding']);
          }

          // For MVP, we route based on their first membership role
          const firstMembership = memberships[0];
          
          if (firstMembership.role === 'admin') {
            return router.createUrlTree(['/employer/dashboard']);
          } else {
            return router.createUrlTree(['/employee/ask']);
          }
        })
      );
    })
  );
};
