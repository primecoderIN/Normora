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

      // Ensure the user's local database profile is created/synced on their very first login via the backend API
      return userService.getMe().pipe(
        map(response => {
          if (!response.success || !response.data) {
            // Backend issue or user not found at all
            return router.createUrlTree(['/auth/login']);
          }

          const memberships = response.data.memberships;

          // Force users with zero workspaces into the onboarding flow where they can accept invites or create an org
          if (memberships.length === 0) {
            return router.createUrlTree(['/onboarding']);
          }

          // Route the user to the appropriate portal (Admin Dashboard vs Employee Chat) based on their highest role in their primary workspace
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
