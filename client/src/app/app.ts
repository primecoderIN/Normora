import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from './core/services/user.service';

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
  // Here we are asking for the OIDC security service to handle auth logic.
  private oidcSecurityService = inject(OidcSecurityService);
  private router = inject(Router);
  private userService = inject(UserService);

  // ngOnInit is a lifecycle hook. It runs exactly once when this component is first created.
  ngOnInit() {
    // When the app starts, or when the user is redirected back from Keycloak,
    // we must call 'checkAuth()' to process the URL parameters and validate the token.
    // The '.subscribe' listens for the result of that check.
    this.oidcSecurityService.checkAuth().subscribe(({ isAuthenticated, userData, accessToken }) => {
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

              if (memberships.length === 0) {
                this.router.navigate(['/onboarding']);
                return;
              }

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
