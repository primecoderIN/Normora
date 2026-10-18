import { AppRoutes } from '@core/constants/app-routes';
import { ApiMessages } from '@core/constants/api-messages';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { environment } from '@env/environment';

/**
 * HTTP interceptor that prefixes API requests with the configured backend URL
 * and provides global HTTP error handling (session redirects, quota notifications, BFLA toasts).
 */
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const messageService = inject(MessageService, { optional: true });
  const router = inject(Router);

  let apiReq = req;
  
  if (req.url.startsWith('/api/') || req.url.startsWith('/bff/')) {
    apiReq = req.clone({
      url: `${environment.apiUrl}${req.url}`
    });
  }

  return next(apiReq).pipe(
    catchError(error => {
      console.error('API Error:', error);
      
      // Centralized error translation based on HTTP response status codes
      switch (error.status) {
        case 401:
          // Unauthenticated or expired session: redirect to login
          router.navigate([AppRoutes.Login]);
          break;

        case 402:
          // Subscription plan limit or quota exceeded: prompt upgrade
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.PlanLimitExceeded.summary,
            detail: error.error?.message || ApiMessages.PlanLimitExceeded.detail,
            life: 6000
          });
          break;

        case 403:
          // Action forbidden due to insufficient role permissions (BFLA protection)
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.Forbidden.summary,
            detail: error.error?.message || ApiMessages.Forbidden.detail,
            life: 5000
          });
          break;

        case 429:
          // Server rate limiting triggered: alert user to throttle requests
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.RateLimit.summary,
            detail: ApiMessages.RateLimit.detail,
            life: 6000
          });
          break;

        case 404:
          // Non-GET resource missing or BOLA authorization rejection
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
          // Internal server or network errors (5xx)
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
