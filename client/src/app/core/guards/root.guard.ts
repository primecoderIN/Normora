import { AppRoutes } from '@core/constants/app-routes';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map, take, switchMap, filter } from 'rxjs/operators';
import { of } from 'rxjs';
import { UserService } from '../services/user.service';

/**
 * Root route guard that directs authenticated users to their primary workspace portal
 * (Employer Dashboard or Employee Ask), unauthenticated visitors to Login,
 * and users without memberships to the Onboarding wizard.
 */
export const rootGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const userService = inject(UserService);

  return authService.isAuthenticated$.pipe(
    // Wait for the initial authentication verification check to resolve from null
    filter(v => v !== null),
    take(1),
    switchMap((isAuthenticated) => {
      if (!isAuthenticated) {
        return of(router.createUrlTree([AppRoutes.Login]));
      }

      // Synchronize and retrieve user profile with tenant memberships
      return userService.getMe().pipe(
        map(response => {
          if (!response.success || !response.data) {
            return router.createUrlTree([AppRoutes.Login]);
          }

          const memberships = response.data.memberships;

          // Route users without existing workspaces to organization setup
          if (memberships.length === 0) {
            return router.createUrlTree(['/onboarding']);
          }

          // Prioritize corporate workspace over personal sandbox for initial landing
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
