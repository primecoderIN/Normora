import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from '../services/user.service';

export const roleGuard: CanActivateFn = (route) => {
  const oidcSecurityService = inject(OidcSecurityService);
  const router = inject(Router);
  const userService = inject(UserService);

  // The required role is passed via the route definition's data property
  const requiredRole = route.data['role'] as string;

  // We rely on the CurrentUser being already fetched in the rootGuard or auth flow
  // However, roleGuard is attached to children. To be safe, we can check the sync value.
  const currentUser = userService.getCurrentUserSync();

  if (!currentUser) {
    return router.createUrlTree(['/auth/login']);
  }

  const hasRole = currentUser.memberships.some(m => {
    // Map admin -> employer, employee -> employee for routing backwards compatibility
    // Our backend sends 'admin' or 'employee' as the role.
    const effectiveRole = m.role === 'admin' ? 'employer' : 'employee';
    return effectiveRole === requiredRole;
  });

  if (hasRole) {
    return true;
  }

  // If they don't have the required role, bounce them to login
  console.warn(`Access denied. Missing role: ${requiredRole}`);
  return router.createUrlTree(['/auth/login']);
};
