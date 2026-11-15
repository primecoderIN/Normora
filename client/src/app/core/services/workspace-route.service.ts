import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { UserService } from './user.service';
import { DsToastService } from '@design-system';

/**
 * Universal service for resolving and actuating workspace-scoped routes from AI tool calls and action cards.
 * Automatically injects active workspace slug and normalizes routes according to user privileges (BFLA defense).
 */
@Injectable({
  providedIn: 'root'
})
export class WorkspaceRouteService {
  private router = inject(Router);
  private userService = inject(UserService);
  private toastService = inject(DsToastService);

  /**
   * Resolves a workspace relative path (e.g. '/employer/documents', '/employee/conversations')
   * into a fully qualified workspace route ('/app/workspaces/:slug/employer/documents') and navigates to it.
   */
  resolveAndNavigate(targetRoute: string, queryParams?: Record<string, any>): void {
    if (!targetRoute) return;

    // Handle external URLs
    if (targetRoute.startsWith('http://') || targetRoute.startsWith('https://')) {
      window.open(targetRoute, '_blank', 'noopener,noreferrer');
      return;
    }

    // If already fully qualified, navigate directly
    if (targetRoute.startsWith('/app/workspaces/')) {
      void this.router.navigateByUrl(targetRoute);
      return;
    }

    // Resolve active workspace slug
    const activeTenant = this.userService.activeTenant();
    let slug = activeTenant?.tenantSlug;

    if (!slug) {
      // Fallback: extract slug from current browser URL
      const match = this.router.url.match(/\/app\/workspaces\/([^/?#]+)/);
      if (match) {
        slug = match[1];
      }
    }

    if (!slug) {
      this.toastService.add({
        severity: 'warn',
        summary: 'Workspace Context Missing',
        detail: 'Unable to resolve the active workspace for this navigation action.'
      });
      return;
    }

    // Determine user role in active workspace ('employer' for admins, 'employee' for employees)
    const effectiveRole = activeTenant?.role?.toLowerCase() === 'admin' ? 'employer' : 'employee';

    let normalizedPath = targetRoute.trim();
    if (!normalizedPath.startsWith('/')) {
      normalizedPath = '/' + normalizedPath;
    }

    // Role-sensitive route normalization:
    // If an employee attempts to navigate to an admin-restricted employer section
    if (effectiveRole === 'employee' && normalizedPath.startsWith('/employer/')) {
      const subPath = normalizedPath.substring('/employer/'.length);

      // Shared features that exist in both employer and employee layouts
      if (subPath.startsWith('conversations')) {
        normalizedPath = '/employee/' + subPath;
      } else if (subPath.startsWith('profile')) {
        normalizedPath = '/employee/profile';
      } else if (subPath.startsWith('saved-answers')) {
        normalizedPath = '/employee/saved-answers';
      } else {
        // Admin-only sections (documents, employees, settings, dashboard)
        this.toastService.add({
          severity: 'warn',
          summary: 'Administrator Privileges Required',
          detail: 'This section requires workspace administrator permissions.'
        });
        return;
      }
    }

    // If an employer is routed to employee-prefixed shared features
    if (effectiveRole === 'employer' && normalizedPath.startsWith('/employee/')) {
      const subPath = normalizedPath.substring('/employee/'.length);
      normalizedPath = '/employer/' + subPath;
    }

    // Ensure leading slash
    if (!normalizedPath.startsWith('/')) {
      normalizedPath = '/' + normalizedPath;
    }

    const fullUrl = `/app/workspaces/${encodeURIComponent(slug)}${normalizedPath}`;

    if (queryParams && Object.keys(queryParams).length > 0) {
      const urlTree = this.router.parseUrl(fullUrl);
      Object.assign(urlTree.queryParams, queryParams);
      void this.router.navigateByUrl(urlTree);
    } else {
      void this.router.navigateByUrl(fullUrl);
    }
  }
}
