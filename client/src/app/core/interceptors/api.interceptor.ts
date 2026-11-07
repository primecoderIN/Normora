import { AppRoutes } from '@core/constants/app-routes';
import { ApiMessages } from '@core/constants/api-messages';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { DsToastService } from '@design-system';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { environment } from '@env/environment';

/**
 * HTTP interceptor that prefixes API requests with the configured backend URL
 * and provides global HTTP error handling (session redirects, quota notifications, BFLA toasts).
 */
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const messageService = inject(DsToastService, { optional: true });
  const router = inject(Router);

  // Prefix all relative API and BFF routes with the configured backend base URL.
  // This allows frontend services to write clean relative paths like '/api/documents'
  // while ensuring outbound HTTP requests resolve to the correct server origin in local development and production.
  let apiReq = req;
  if (req.url.startsWith('/api/') || req.url.startsWith('/bff/')) {
    apiReq = req.clone({
      url: `${environment.apiUrl}${req.url}`
    });
  }

  return next(apiReq).pipe(
    catchError(error => {
      console.error('API Error:', error);
      
      // Centralized error normalization: intercept status codes globally so UI components
      // do not need repetitive try/catch logic for standard auth, quota, or permission failures.
      switch (error.status) {
        case 401:
          // Unauthenticated or expired session cookie.
          // Direct the browser to the login route, initiating the Duende BFF challenge handshake with Keycloak.
          router.navigate([AppRoutes.Login]);
          break;

        case 402:
          // Plan quota exceeded (e.g., employee seats, document count, or storage limit).
          // Display a prominent warning toast prompting the employer to upgrade their subscription tier.
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.PlanLimitExceeded.summary,
            detail: error.error?.message || ApiMessages.PlanLimitExceeded.detail,
            life: 6000
          });
          break;

        case 403:
          // Broken Function Level Authorization (BFLA) violation.
          // Triggered when an authenticated user lacks the required workspace role (e.g., an Employee attempting to invite team members).
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.Forbidden.summary,
            detail: error.error?.message || ApiMessages.Forbidden.detail,
            life: 5000
          });
          break;

        case 429:
          // Rate-limiting middleware triggered (e.g., sliding window limits on expensive Gemini AI conversational turns).
          // Notify the user to pause briefly before retrying their request.
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.RateLimit.summary,
            detail: ApiMessages.RateLimit.detail,
            life: 6000
          });
          break;

        case 404:
          // Broken Object Level Authorization (BOLA) or missing resource.
          // For GET requests, let components handle 404s natively (e.g., displaying empty lists or search results).
          // For mutating requests (POST, PUT, DELETE), surface a warning toast indicating the target was removed or cross-tenant access was blocked.
          if (req.method !== 'GET' && messageService) {
            messageService.add({
              severity: 'warn',
              summary: ApiMessages.NotFound.summary,
              detail: error.error?.message || ApiMessages.NotFound.detail,
              life: 4000
            });
          }
          break;

        default:
          // Server errors (500-599) or unhandled gateway timeouts.
          // Display a generic server error toast without exposing raw stack traces or internal server error strings.
          if (error.status >= 500 && messageService) {
            messageService.add({
              severity: 'error',
              summary: ApiMessages.ServerError.summary,
              detail: ApiMessages.ServerError.detail,
              life: 6000
            });
          }
          break;
      }

      return throwError(() => error);
    })
  );
};
