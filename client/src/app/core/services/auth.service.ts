import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  
  private authStatus = new BehaviorSubject<boolean | null>(null);
  public isAuthenticated$ = this.authStatus.asObservable();

  // Duende BFF provides the logout URL (with the anti-forgery `sid` parameter)
  // as a claim in the /bff/user response. We store it here for use in logout().
  private logoutUrl: string | null = null;

  public checkAuth(): Observable<boolean> {
    return this.http.get<any[]>('/bff/user').pipe(
      map(claims => {
        const isAuth = Array.isArray(claims) && claims.length > 0;
        this.authStatus.next(isAuth);

        // Extract the logout URL from the BFF claims
        if (isAuth) {
          const logoutClaim = claims.find(c => c.type === 'bff:logout_url');
          this.logoutUrl = logoutClaim?.value ?? '/bff/logout';
        }

        return isAuth;
      }),
      catchError(() => {
        this.authStatus.next(false);
        return of(false);
      })
    );
  }

  public login(): void {
    window.location.href = '/bff/login';
  }

  public logout(): void {
    // Navigate to the BFF logout URL which includes the `sid` query parameter
    // for anti-forgery protection. This triggers server-side session cleanup
    // and redirects to Keycloak's end_session endpoint.
    window.location.href = this.logoutUrl ?? '/bff/logout';
  }
}

