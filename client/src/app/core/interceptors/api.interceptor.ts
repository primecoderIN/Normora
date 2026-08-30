import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MessageService } from 'primeng/api';
import { catchError, switchMap, take } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const oidcSecurityService = inject(OidcSecurityService);
  // Optional inject because not all components provide MessageService
  const messageService = inject(MessageService, { optional: true });

  // Wait, angular-auth-oidc-client provides its own interceptor, but we can write a custom one 
  // to also handle errors generically via MessageService.
  // Actually, we already configure the authInterceptor in app.config.ts, so we might just need an error interceptor.
  return next(req).pipe(
    catchError(error => {
      console.error('API Error:', error);
      
      if (messageService && req.method !== 'GET') {
        if (error.status >= 500) {
          messageService.add({ severity: 'error', summary: 'Server Error', detail: 'An unexpected error occurred.' });
        }
      }
      return throwError(() => error);
    })
  );
};
