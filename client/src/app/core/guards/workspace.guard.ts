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

  // Path-based workspace slug extraction:
  // Reads the ':slug' parameter from the URL (e.g., /app/workspaces/acme/employer/dashboard).
  // If missing or unparseable, safely route back to the login screen.
  const slug = route.paramMap.get('slug');
  if (!slug) {
    return router.createUrlTree([AppRoutes.Login]);
  }

  const currentUser = userService.currentUser();

  const validateAccess = (user: CurrentUser | null) => {
    // Unauthenticated user: redirect to login to trigger the BFF authentication flow.
    if (!user) return router.createUrlTree([AppRoutes.Login]);

    // BOLA verification on the client tier:
    // Ensure the authenticated user holds an active membership matching this exact workspace slug.
    const membership = user.memberships.find((m: UserTenantMembership) => m.tenantSlug === slug);
    if (!membership) {
      // If the user is logged in but lacks access to this tenant, redirect to AccessDenied.
      // Redirecting to Login would trigger a session loop because the user is already authenticated.
      console.warn(`Access denied. User is not a member of workspace: ${slug}`);
      return router.createUrlTree([AppRoutes.AccessDenied]);
    }

    // Set active workspace context in the global Signal and dynamically inject white-label branding CSS variables.
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
