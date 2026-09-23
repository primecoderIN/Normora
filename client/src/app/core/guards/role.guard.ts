import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserService } from '../services/user.service';
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

  const validateRole = (user: any) => {
    if (!user) return router.createUrlTree(['/auth/login']);
    
    const hasRole = user.memberships.some((m: any) => {
      const effectiveRole = m.role?.toLowerCase() === 'admin' ? 'employer' : 'employee';
      return effectiveRole === requiredRole;
    });

    if (hasRole) return true;

    console.warn(`Access denied. Missing role: ${requiredRole}`);
    return router.createUrlTree(['/auth/login']);
  };

  if (currentUser) {
    return validateRole(currentUser);
  } else {
    return userService.getMe().pipe(
      map(res => validateRole(res.success ? res.data : null))
    );
  }
};
