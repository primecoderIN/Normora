import { AppRoutes } from '@core/constants/app-routes';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserService, CurrentUser, UserTenantMembership } from '../services/user.service';
import { TenantBrandingService } from '../services/tenant-branding.service';
import { map } from 'rxjs/operators';

/**
 * Route guard that resolves the active workspace by slug from route parameters.
 * Validates the authenticated user's tenant membership, sets the activeTenantId signal,
 * dynamically loads and applies the tenant's custom branding, or redirects to access-denied.
 */
export const workspaceGuard: CanActivateFn = (route) => {
  const userService = inject(UserService);
  const brandingService = inject(TenantBrandingService);
  const router = inject(Router);

  const slug = route.paramMap.get('slug');
  if (!slug) {
    return router.createUrlTree([AppRoutes.Login]);
  }

  const currentUser = userService.currentUser();

  const validateAccess = (user: CurrentUser | null) => {
    if (!user) return router.createUrlTree([AppRoutes.Login]);

    const membership = user.memberships.find((m: UserTenantMembership) => m.tenantSlug === slug);
    if (!membership) {
      // Authenticated user lacks membership in this workspace; redirect to access-denied to prevent redirect loops
      console.warn(`Access denied. User is not a member of workspace: ${slug}`);
      return router.createUrlTree([AppRoutes.AccessDenied]);
    }

    // Set active workspace context and bootstrap white-label CSS variables
    userService.activeTenantId.set(membership.tenantId);
    brandingService.applyBrandingForSlug(slug).subscribe();
    return true;
  };

  if (currentUser) {
    return validateAccess(currentUser);
  } else {
    return userService.getMe().pipe(
      map(res => validateAccess(res.success ? res.data : null))
    );
  }
};
