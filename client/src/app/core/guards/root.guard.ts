import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, take, switchMap, filter } from 'rxjs/operators';
import { of } from 'rxjs';
import { UserService } from '../services/user.service';

export const rootGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const userService = inject(UserService);

  return authService.isAuthenticated$.pipe(
    // BUG-3: Wait until authentication state is definitely known (not null)
    // before taking a value, to prevent incorrect redirects on cold first load.
    filter(v => v !== null),
    take(1),
    switchMap((isAuthenticated) => {
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

          // SEC-15: Use the first non-personal workspace for routing decisions.
          // Falling back to memberships[0] could route a user with a personal workspace
          // as their first entry to the employer dashboard incorrectly.
          const primaryMembership = memberships.find(m => !m.isPersonal) ?? memberships[0];

          if (primaryMembership.role === 'admin') {
            return router.createUrlTree(['/employer/dashboard']);
          } else {
            return router.createUrlTree(['/employee/ask']);
          }
        })
      );
    })
  );
};
