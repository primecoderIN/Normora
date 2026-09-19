import { HttpInterceptorFn } from '@angular/common/http';

export const csrfInterceptor: HttpInterceptorFn = (req, next) => {
  // Duende BFF uses 'X-CSRF: 1' as the anti-forgery token.
  // It is required for ALL mutating requests (POST, PUT, DELETE, PATCH)
  // AND for ALL requests to BFF management endpoints (like GET /bff/user)
  // AND for ALL requests to API endpoints protected by AsBffApiEndpoint() (like GET /api/users/me)
  const isMutatingRequest = ['POST', 'PUT', 'DELETE', 'PATCH'].includes(req.method.toUpperCase());
  const isBffEndpoint = req.url.startsWith('/bff/');
  const isApiEndpoint = req.url.startsWith('/api/');

  if (isMutatingRequest || isBffEndpoint || isApiEndpoint) {
    const csrfReq = req.clone({
      headers: req.headers.set('X-CSRF', '1')
    });
    return next(csrfReq);
  }
  return next(req);
};
