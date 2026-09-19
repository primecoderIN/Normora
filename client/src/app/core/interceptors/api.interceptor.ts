import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { environment } from '@env/environment';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  // Optional inject because not all components provide MessageService
  const messageService = inject(MessageService, { optional: true });

  let apiReq = req;
  
  if (req.url.startsWith('/api/') || req.url.startsWith('/bff/')) {
    apiReq = req.clone({
      url: `${environment.apiUrl}${req.url}`
    });
  }

  return next(apiReq).pipe(
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
