import { Component, inject } from '@angular/core';

import { OidcSecurityService } from 'angular-auth-oidc-client';
import { ButtonDirective } from 'primeng/button';
import { ProgressSpinner } from 'primeng/progressspinner';

// This is the Login Component. It is responsible for rendering the login page
// and starting the authentication process.
@Component({
  selector: 'app-login',
  standalone: true,
  // We import the PrimeNG components so we can use them in the HTML template
  imports: [ButtonDirective, ProgressSpinner],
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login {
  // Inject the security service so we can talk to Keycloak
  private oidcSecurityService = inject(OidcSecurityService);

  // We are processing a login if there is a 'code' or 'state' in the URL,
  // which indicates we just returned from Keycloak.
  get isProcessingLogin(): boolean {
    return window.location.search.includes('code=') || window.location.search.includes('state=');
  }

  // This method is called when the user clicks the "Sign In" button in the HTML
  login() {
    // authorize() tells the library to construct the Keycloak login URL
    // and redirect the user's browser there.
    this.oidcSecurityService.authorize();
  }

  // This method tells Keycloak to skip its own login page and go straight to GitHub
  loginWithGithub() {
    console.log('Attempting GitHub login...');

    try {
      this.oidcSecurityService.authorize(undefined, {
        customParams: {
          kc_idp_hint: 'github', // This must match the Alias you gave the provider in Keycloak
        },
      });
    } catch (e) {
      console.error('Error during authorize call:', e);
    }
  }

  // This method tells Keycloak to skip its own login page and go straight to Google
  loginWithGoogle() {
    console.log('Attempting Google login...');

    try {
      this.oidcSecurityService.authorize(undefined, {
        customParams: {
          kc_idp_hint: 'google', // This must match the Alias you gave the provider in Keycloak
        },
      });
    } catch (e) {
      console.error('Error during authorize call:', e);
    }
  }
}
