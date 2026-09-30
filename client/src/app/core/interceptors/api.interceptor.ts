import { AppRoutes } from '@core/constants/app-routes';
import { ApiMessages } from '@core/constants/api-messages';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { environment } from '@env/environment';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  // Optional inject because not all components provide MessageService
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
      
      // SEC-14 / UX-3: Centralized error handling for common HTTP status codes
      switch (error.status) {
        case 401:
          // Session expired or unauthenticated — redirect to login
          router.navigate([AppRoutes.Login]);
          break;

        case 403:
          // Authenticated but not authorized (BFLA) — show toast
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.Forbidden.summary,
            detail: error.error?.message || ApiMessages.Forbidden.detail,
            life: 5000
          });
          break;

        case 429:
          // UX-7: Rate limit hit — show a user-friendly message
          messageService?.add({
            severity: 'warn',
            summary: ApiMessages.RateLimit.summary,
            detail: ApiMessages.RateLimit.detail,
            life: 6000
          });
          break;

        case 404:
          // For BOLA or missing resources, show toast unless it's a routine GET that shouldn't alert
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
          // Generic server errors (5xx)
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
