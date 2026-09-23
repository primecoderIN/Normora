import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { UserService } from './core/services/user.service';
import { TenantBrandingService } from './core/services/tenant-branding.service';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { NotificationRealtimeService } from './core/services/notification-realtime.service';
import { TenantRoles } from './core/constants/tenant-roles';

// This is the Root Component of our Angular application. 
// Think of it as the main container that holds everything else.
@Component({
  // The 'imports' array allows us to use other standalone components/directives in this component.
  // We need RouterOutlet so Angular knows where to render the page content based on the URL.
  imports: [RouterOutlet, ToastModule],
  providers: [MessageService],
  selector: 'app-root',

  templateUrl: './app.html',
})
export class App implements OnInit {
  // A 'signal' is a new Angular feature for managing state reactively.
  protected readonly title = signal('client');
  
  // 'inject' is the modern way to request a service from Angular's Dependency Injection system.
  private authService = inject(AuthService);
  private router = inject(Router);
  private userService = inject(UserService);
  private brandingService = inject(TenantBrandingService);
  private messageService = inject(MessageService);
  private notificationService = inject(NotificationRealtimeService);

  protected readonly authInitializing = signal(true);
  protected readonly authStatus = signal('Checking your sign-in...');

  // ngOnInit is a lifecycle hook. It runs exactly once when this component is first created.
  ngOnInit() {
    // Step 1: Subdomain routing has been removed in favor of path-based workspace routing.
    // Branding is now applied after authentication in the workspace routing logic.

    // Step 2: Check authentication state via BFF.
    this.authService.checkAuth().subscribe({
      next: (isAuthenticated) => {
      if (isAuthenticated) {
        // Connect to realtime notifications
        this.notificationService.connect();

        const currentPath = window.location.pathname;
        const isAuthRoute =
          currentPath === '/' ||
          currentPath.startsWith('/auth/login') ||
          currentPath.startsWith('/auth/callback') ||
          currentPath.startsWith('/signin-oidc');

        if (isAuthRoute || !this.userService.currentUser()) {
          this.authStatus.set('Loading your workspace...');

          this.userService.getMe().subscribe({
            next: (response) => {
              if (!response.success || !response.data) {
                this.authStatus.set('We could not load your profile. Returning to sign in...');
                this.authService.logout();
                this.authInitializing.set(false);
                return;
              }

              const memberships = response.data.memberships;

              if (!isAuthRoute) {
                this.authInitializing.set(false);
                return;
              }

              const pendingToken = localStorage.getItem('pending_invitation');
              if (pendingToken) {
                this.authInitializing.set(false);
                this.router.navigate(['/accept-invite'], { queryParams: { token: pendingToken } });
                return;
              }

              if (memberships.length === 0) {
                this.authInitializing.set(false);
                this.router.navigate(['/onboarding']);
                return;
              }

              // Determine the default workspace (prefer non-personal)
              const defaultMembership = memberships.find(m => !m.isPersonal) || memberships[0];
              const tenantSlug = defaultMembership.tenantSlug;
              const isAdmin = defaultMembership.role === TenantRoles.Admin;
              const targetPath = isAdmin ? '/employer/dashboard' : '/employee/conversations';

              // Route user to their workspace
              if (tenantSlug) {
                // Apply branding directly
                this.brandingService.applyBrandingForSlug(tenantSlug).subscribe();
                
                // Navigate to the workspace path
                const fullPath = `/app/workspaces/${tenantSlug}${targetPath}`;
                this.authInitializing.set(false);
                this.router.navigateByUrl(fullPath);
              } else {
                // Fallback if no memberships (should rarely happen if backend creates personal workspace)
                this.authInitializing.set(false);
                this.router.navigate([targetPath]);
              }
            },
            error: () => {
              this.authStatus.set('We could not verify your account. Returning to sign in...');
              this.authService.logout();
              this.authInitializing.set(false);
            }
          });
        } else {
          this.authInitializing.set(false);
        }
      } else if (window.location.pathname.startsWith('/auth/callback') || window.location.pathname.startsWith('/signin-oidc')) {
        this.authStatus.set('Sign-in could not be completed. Returning to sign in...');
        this.authInitializing.set(false);
        this.router.navigate(['/auth/login']);
      } else {
        this.authInitializing.set(false);
      }
      },
      error: () => {
        this.authStatus.set('Sign-in could not be completed. Returning to sign in...');
        this.authInitializing.set(false);
        this.router.navigate(['/auth/login']);
      }
    });

    // Step 3: Listen for real-time notifications
    this.notificationService.invitationReceived$.subscribe(event => {
      this.messageService.add({
        severity: 'info',
        summary: 'New Invitation',
        detail: `You have been invited to join ${event.tenantName}!`,
        life: 10000 // 10 seconds
      });
    });
  }

  ngOnDestroy() {
    this.notificationService.disconnect();
  }
}
