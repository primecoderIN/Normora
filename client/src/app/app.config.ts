import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { provideAuth, authInterceptor, LogLevel } from 'angular-auth-oidc-client';
import { tenantInterceptor } from './core/interceptors/tenant.interceptor';

// The appConfig is the central configuration file for our standalone Angular application.
// It tells Angular what global services (providers) should be available everywhere in the app.
export const appConfig: ApplicationConfig = {
  providers: [
    // Catches unhandled errors globally in the browser
    provideBrowserGlobalErrorListeners(),
    
    // Sets up our routing system using the routes defined in app.routes.ts
    provideRouter(routes),
    
    // Configures the HTTP Client used to make API calls to the backend.
    // We attach the 'authInterceptor' here. An interceptor acts like a middleman:
    // every time our app sends an HTTP request, the interceptor catches it, 
    // attaches our Keycloak Access Token to the headers, and then sends it on its way.
    provideHttpClient(withInterceptors([authInterceptor(), tenantInterceptor])),
    
    // Configures our Authentication service (OIDC - OpenID Connect).
    provideAuth({
      config: {
        // The URL of our Keycloak server. This is where the app goes to verify identities.
        authority: `${environment.keycloakUrl}/realms/normora`,

        // A DEDICATED callback route — Keycloak redirects here after login.
        // Using /auth/callback (not /auth/login) prevents the login form from
        // briefly flashing during the OAuth round-trip.
        redirectUrl: window.location.origin + '/auth/callback',

        // Where Keycloak should send the user after logging out
        postLogoutRedirectUri: window.location.origin + '/auth/login',

        // The ID of this application registered inside Keycloak
        clientId: 'normora-web',

        // 'openid' is required for OIDC. 'profile' and 'email' give us user details.
        scope: 'openid profile email',

        // Authorization Code Flow — most secure for public browser apps
        responseType: 'code',

        // PKCE (Proof Key for Code Exchange) is ON by default in this library —
        // no property needed. The Keycloak client is also enforcing S256 server-side.
        // To explicitly disable PKCE (never do this) you would set: disablePkce: true

        // Silently renew the access token before it expires using the refresh token
        silentRenew: true,
        useRefreshToken: true,

        // Required when useRefreshToken=true: prevents nonce mismatch errors
        // during background token renewal (nonce is only validated on first login)
        ignoreNonceAfterRefresh: true,

        // Proactively renew 30 seconds before expiry to avoid 401s on in-flight requests
        renewTimeBeforeTokenExpiresInSeconds: 30,

        // Handle missing 'unauthorized' route error
        unauthorizedRoute: '/auth/login',
        forbiddenRoute: '/auth/login',

        // Only suppress logs in production; warn level is safe for dev
        logLevel: LogLevel.Warn,

        // ONLY attach our Bearer token when calling our own API.
        // This prevents accidentally leaking the token to third-party services.
        secureRoutes: ['http://localhost:5000/api/', '/api/']
      }
    }),
    
    // Configures PrimeNG (our UI component library) with the Aura theme
    providePrimeNG({
      license: environment.primeNgLicense,
      theme: {
        preset: Aura,
        options: {
          cssLayer: {
            name: 'primeng',
            order: 'theme, base, primeng'
          }
        }
      }
    })
  ],
};
