import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from './core/services/user.service';
import { TenantBrandingService } from './core/services/tenant-branding.service';

// This is the Root Component of our Angular application. 
// Think of it as the main container that holds everything else.
@Component({
  // The 'imports' array allows us to use other standalone components/directives in this component.
  // We need RouterOutlet so Angular knows where to render the page content based on the URL.
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  // A 'signal' is a new Angular feature for managing state reactively.
  protected readonly title = signal('client');
  
  // 'inject' is the modern way to request a service from Angular's Dependency Injection system.
  private oidcSecurityService = inject(OidcSecurityService);
  private router = inject(Router);
  private userService = inject(UserService);
  private brandingService = inject(TenantBrandingService);

  // ngOnInit is a lifecycle hook. It runs exactly once when this component is first created.
  ngOnInit() {
    // Step 1: Fetch and apply tenant branding based on the current subdomain (if any).
    // e.g. on intel.localhost:4200 we load Intel's colors/favicon before the login page renders.
    // The app always stays on localhost:4200 — subdomain is only used for branding context.
    const slug = this.brandingService.getSlugFromSubdomain();
    if (slug) {
      this.brandingService.applyBrandingForSlug(slug).subscribe();
    }

    // Step 2: Check authentication state.
    this.oidcSecurityService.checkAuth().subscribe(({ isAuthenticated }) => {
      if (isAuthenticated) {

        // Route to dashboard when the user lands on login, callback, or root.
        // The /auth/callback route is where Keycloak redirects after OAuth.
        const isAuthRoute =
          this.router.url === '/' ||
          this.router.url.startsWith('/auth/login') ||
          this.router.url.startsWith('/auth/callback');

        if (isAuthRoute) {
          // We now rely on our backend DB for roles (Memberships), not Keycloak realm_access
          this.userService.getMe().subscribe({
            next: (response) => {
              if (!response.success || !response.data) {
                this.oidcSecurityService.logoffLocal();
                this.router.navigate(['/auth/login']);
                return;
              }

              const memberships = response.data.memberships;
              
              // Check if user just logged in specifically to accept an invite
              const pendingToken = localStorage.getItem('pending_invitation');
              if (pendingToken) {
                this.router.navigate(['/accept-invite'], { queryParams: { token: pendingToken } });
                return;
              }

              // No tenants: go to onboarding.
              if (memberships.length === 0) {
                this.router.navigate(['/onboarding']);
                return;
              }

              // Route based on role. App always stays on localhost:4200.
              const firstMembership = memberships[0];
              if (firstMembership.role === 'admin') {
                this.router.navigate(['/employer/dashboard']);
              } else {
                this.router.navigate(['/employee/ask']);
              }
            },
            error: () => {
              // API failure or unauthorized
              this.oidcSecurityService.logoffLocal();
              this.router.navigate(['/auth/login']);
            }
          });
        }
      }
    });
  }
}
