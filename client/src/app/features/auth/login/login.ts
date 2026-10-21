import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/user.service';
import { TenantRoles } from '../../../core/constants/tenant-roles';
import { AppRoutes } from '../../../core/constants/app-routes';

/**
 * Authentication entry component handling Keycloak login redirect
 * and automatic routing for users already authenticated.
 */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [],
  templateUrl: './login.html',
})
export class Login implements OnInit {
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private router = inject(Router);

  ngOnInit() {
    // If user is already authenticated, route directly to their default workspace
    if (this.authService.isAuthenticated || this.userService.currentUser()) {
      const activeTenant = this.userService.activeTenant();
      const user = this.userService.currentUser();
      if (activeTenant) {
        const membership = user?.memberships?.find(m => m.tenantId === activeTenant.tenantId);
        const isAdmin = membership?.role === TenantRoles.Admin;
        const target = isAdmin
          ? AppRoutes.WorkspaceEmployerDashboard(activeTenant.tenantSlug)
          : AppRoutes.WorkspaceEmployeeConversations(activeTenant.tenantSlug);
        this.router.navigateByUrl(target);
        return;
      }

      if (user?.memberships && user.memberships.length > 0) {
        const defaultMembership = user.memberships.find(m => !m.isPersonal) || user.memberships[0];
        const isAdmin = defaultMembership.role === TenantRoles.Admin;
        const slug = defaultMembership.tenantSlug;
        if (slug) {
          const target = isAdmin
            ? AppRoutes.WorkspaceEmployerDashboard(slug)
            : AppRoutes.WorkspaceEmployeeConversations(slug);
          this.router.navigateByUrl(target);
          return;
        }
      }
    }
  }

  login(): void {
    this.authService.login();
  }
}


