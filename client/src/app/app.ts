import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

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
          // Roles live in the Access Token (realm_access.roles), NOT in UserInfo.
          this.oidcSecurityService.getPayloadFromAccessToken().subscribe((payload) => {
            const roles: string[] = payload?.realm_access?.roles ?? [];

            if (roles.includes('employer')) {
              this.router.navigate(['/employer/dashboard']);
            } else if (roles.includes('employee')) {
              this.router.navigate(['/employee/ask']);
            } else {
              console.error(
                'Auth: user is authenticated but has no employer/employee role.',
                'Check Keycloak role mappers for this user or identity provider.',
                payload
              );
              // User has a stale token without roles (or role mapper is missing).
              // We must clear their local session and send them back to login, 
              // otherwise they get stuck on the callback spinner forever.
              this.oidcSecurityService.logoffLocal();
              this.router.navigate(['/auth/login']);
            }
          });
        }
      }
    });
  }
}
