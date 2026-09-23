import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserService } from '../services/user.service';
import { TenantBrandingService } from '../services/tenant-branding.service';
import { map } from 'rxjs/operators';

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
  
  const validateAccess = (user: any) => {
    if (!user) return router.createUrlTree(['/auth/login']);
    const membership = user.memberships.find((m: any) => m.tenantSlug === slug);
    if (!membership) {
      console.warn(`Access denied. User is not a member of workspace: ${slug}`);
      return router.createUrlTree(['/auth/login']);
    }
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
