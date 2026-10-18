import { AppRoutes } from '@core/constants/app-routes';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { filter, map, take } from 'rxjs/operators';

/**
 * Route guard enforcing user authentication across protected application routes.
 * Awaits resolved session state from AuthService and redirects unauthenticated requests to login.
 */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticated$.pipe(
    filter(isAuthenticated => isAuthenticated !== null),
    take(1),
    map((isAuthenticated) => {
      if (isAuthenticated) {
        return true;
      }
      return router.createUrlTree([AppRoutes.Login]);
    })
  );
};
