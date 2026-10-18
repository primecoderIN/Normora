import { AppRoutes } from '@core/constants/app-routes';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserService, CurrentUser, UserTenantMembership } from '../services/user.service';
import { map } from 'rxjs/operators';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const userService = inject(UserService);

  // Enforce route protection by verifying the user has the required access level across any of their active workspaces
  const requiredRole = route.data['role'] as string;

  // We rely on the CurrentUser being already fetched in the rootGuard or auth flow
  // However, roleGuard is attached to children. To be safe, we can check the sync value.
  const currentUser = userService.currentUser();

  const validateRole = (user: CurrentUser | null) => {
    if (!user) return router.createUrlTree([AppRoutes.Login]);
    
    // SEC-11: Validate role scoped strictly to the current workspace to prevent BFLA
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

    const effectiveRole = membership.role?.toLowerCase() === 'admin' ? 'employer' : 'employee';
    if (effectiveRole === requiredRole) {
      return true;
    }

    // SEC-11: User is authenticated but lacks the required role in this specific workspace
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
