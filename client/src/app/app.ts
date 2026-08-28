import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
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

  // ngOnInit is a lifecycle hook. It runs exactly once when this component is first created.
  ngOnInit() {
    // When the app starts, or when the user is redirected back from Keycloak,
    // we must call 'checkAuth()' to process the URL parameters and validate the token.
    // The '.subscribe' listens for the result of that check.
    this.oidcSecurityService.checkAuth().subscribe(({ isAuthenticated, userData, accessToken }) => {
      console.log('App authenticated:', isAuthenticated);
      
      // If the user is successfully logged in, we log their profile data (from Keycloak) to the console.
      if (isAuthenticated) {
        console.log('User data:', userData);
      }
    });
  }
}
