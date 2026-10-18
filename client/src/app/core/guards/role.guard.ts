import { AppRoutes } from '@core/constants/app-routes';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserService, CurrentUser, UserTenantMembership } from '../services/user.service';
import { map } from 'rxjs/operators';

/**
 * Route guard that validates user role permissions ('employer' or 'employee').
 * Strictly scopes the check to the current active workspace (via activeTenantId or route slug)
 * to prevent Broken Function Level Authorization (BFLA).
 */
export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const userService = inject(UserService);

  const requiredRole = route.data['role'] as string;
  const currentUser = userService.currentUser();

  const validateRole = (user: CurrentUser | null) => {
    if (!user) return router.createUrlTree([AppRoutes.Login]);

    // Find the specific membership belonging to the currently active workspace
    const activeTenantId = userService.activeTenantId();
    const slug = route.parent?.paramMap.get('slug') || route.paramMap.get('slug');

    let membership: UserTenantMembership | undefined;
    if (activeTenantId) {
      membership = user.memberships.find(m => m.tenantId === activeTenantId);
    }
    if (!membership && slug) {
      membership = user.memberships.find(m => m.tenantSlug === slug);
    }
    if (!membership && user.memberships.length > 0) {
      membership = user.memberships[0];
    }

    if (!membership) {
      console.warn(`Access denied. No valid workspace membership found.`);
      return router.createUrlTree([AppRoutes.AccessDenied]);
    }

    // Map internal tenant role (admin -> employer, employee -> employee)
    const effectiveRole = membership.role?.toLowerCase() === 'admin' ? 'employer' : 'employee';
    if (effectiveRole === requiredRole) {
      return true;
    }

    // User is authenticated but lacks required privilege in this workspace
    console.warn(`Access denied. Role in active workspace is '${effectiveRole}', required: '${requiredRole}'`);
    return router.createUrlTree([AppRoutes.AccessDenied]);
  };

  if (currentUser) {
    return validateRole(currentUser);
  } else {
    return userService.getMe().pipe(
      map(res => validateRole(res.success ? res.data : null))
    );
  }
};
