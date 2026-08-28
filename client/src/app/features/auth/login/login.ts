import { Component, inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { ButtonModule } from 'primeng/button';

// This is the Login Component. It is responsible for rendering the login page
// and starting the authentication process.
@Component({
  selector: 'app-login',
  standalone: true,
  // We import the PrimeNG ButtonModule so we can use <p-button> in the HTML template
  imports: [ButtonModule],
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login {
  // Inject the security service so we can talk to Keycloak
  private oidcSecurityService = inject(OidcSecurityService);

  // This method is called when the user clicks the "Sign In" button in the HTML
  login() {
    // authorize() tells the library to construct the Keycloak login URL 
    // and redirect the user's browser there.
    this.oidcSecurityService.authorize();
  }
}
