import { AppRoutes } from '@core/constants/app-routes';
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
          // Authenticated but not authorized — show toast and redirect to access-denied
          messageService?.add({
            severity: 'warn',
            summary: 'Access Denied',
            detail: 'You do not have permission to perform this action.',
            life: 5000
          });
          break;

        case 429:
          // UX-7: Rate limit hit — show a user-friendly message
          messageService?.add({
            severity: 'warn',
            summary: 'Slow Down',
            detail: 'You are sending requests too fast. Please wait a moment and try again.',
            life: 6000
          });
          break;

        case 404:
          // Only show 404 toasts for non-GET requests (e.g., delete a resource that no longer exists)
          if (req.method !== 'GET' && messageService) {
            messageService.add({
              severity: 'warn',
              summary: 'Not Found',
              detail: 'The requested resource was not found.',
              life: 4000
            });
          }
          break;

        default:
          // Generic server errors (5xx)
          if (error.status >= 500 && messageService) {
            messageService.add({
              severity: 'error',
              summary: 'Server Error',
              detail: 'An unexpected error occurred. Please try again later.',
              life: 6000
            });
          }
          break;
      }

      return throwError(() => error);
    })
  );
};
