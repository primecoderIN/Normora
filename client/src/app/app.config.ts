import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import {
  PreloadAllModules,
  provideRouter,
  withComponentInputBinding,
  withPreloading,
} from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { routes } from './app.routes';
import { environment } from '@env/environment';
import { tenantInterceptor } from '@core/interceptors/tenant.interceptor';
import { apiInterceptor } from '@core/interceptors/api.interceptor';
import { csrfInterceptor } from '@core/interceptors/csrf.interceptor';
import { provideClientHydration } from '@angular/platform-browser';

// The appConfig is the central configuration file for our standalone Angular application.
// It tells Angular what global services (providers) should be available everywhere in the app.
export const appConfig: ApplicationConfig = {
  providers: [
    // Catches unhandled errors globally in the browser
    provideBrowserGlobalErrorListeners(),

    // Sets up our routing system. withPreloading eagerly downloads lazy chunks in
    // the background after the initial page loads, so navigating between routes is instant.
    // withComponentInputBinding allows route params/data to be bound directly via @Input().
    provideRouter(routes, withPreloading(PreloadAllModules), withComponentInputBinding()),

    // Configures the HTTP Client used to make API calls to the backend.
    // We attach our interceptors here to automatically attach the CSRF header and Tenant ID.
    provideHttpClient(withInterceptors([csrfInterceptor, tenantInterceptor, apiInterceptor])),

    // Configures PrimeNG (our UI component library) with the Aura theme
    providePrimeNG({
      license: environment.primeNgLicense,
      theme: {
        preset: Aura,
        options: {
          cssLayer: {
            name: 'primeng',
            order: 'theme, base, primeng',
          },
        },
      },
    }),
    provideClientHydration(),
  ],
};
