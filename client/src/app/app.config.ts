import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { provideAuth, authInterceptor } from 'angular-auth-oidc-client';

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
    provideHttpClient(withInterceptors([authInterceptor()])),
    
    // Configures our Authentication service (OIDC - OpenID Connect).
    provideAuth({
      config: {
        // The URL of our Keycloak server. This is where the app goes to verify identities.
        authority: 'http://localhost:8080/realms/normora',
        
        // Where Keycloak should send the user back to after a successful login
        redirectUrl: window.location.origin,
        
        // Where Keycloak should send the user back to after logging out
        postLogoutRedirectUri: window.location.origin,
        
        // The ID of this specific application registered inside Keycloak
        clientId: 'normora-web',
        
        // The 'scopes' define what information we are asking Keycloak for.
        // 'openid' is required for OIDC. 'profile' and 'email' give us user details.
        scope: 'openid profile email offline_access',
        
        // 'code' flow is the most secure modern OAuth2 flow for browser apps (PKCE)
        responseType: 'code',
        
        // Automatically try to get a new token in the background before the current one expires
        silentRenew: true,
        useRefreshToken: true,
        
        // This is crucial: It tells the interceptor ONLY to attach our secret token 
        // when we are talking to our own API. We don't want to accidentally send 
        // our token to a random third-party API!
        secureRoutes: ['http://localhost:5000/api/', '/api/']
      }
    }),
    
    // Configures PrimeNG (our UI component library) with the Aura theme
    providePrimeNG({
      license: environment.primeNgLicense,
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: '.dark',
        },
      },
    }),
  ],
};
