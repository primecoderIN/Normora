import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserService } from '../services/user.service';
import { TenantBrandingService } from '../services/tenant-branding.service';

export const workspaceGuard: CanActivateFn = (route) => {
  const userService = inject(UserService);
  const brandingService = inject(TenantBrandingService);
  const router = inject(Router);
  
  // Extract slug from route params
  const slug = route.paramMap.get('slug');
  
  if (!slug) {
    return router.createUrlTree(['/auth/login']);
  }
  
  const currentUser = userService.currentUser();
  
  if (!currentUser) {
    // If somehow we hit this guard and aren't loaded, return to login
    return router.createUrlTree(['/auth/login']);
  }
  
  // Validate that the user belongs to this workspace
  const membership = currentUser.memberships.find(m => m.tenantSlug === slug);
  
  if (!membership) {
    // User doesn't have access to this workspace
    console.warn(`Access denied. User is not a member of workspace: ${slug}`);
    return router.createUrlTree(['/auth/login']);
  }
  
  // Set the active tenant for interceptors
  userService.activeTenantId.set(membership.tenantId);
  
  // Apply branding for the workspace
  brandingService.applyBrandingForSlug(slug).subscribe();
  
  return true;
};
