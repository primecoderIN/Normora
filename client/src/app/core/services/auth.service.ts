import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, of } from 'rxjs';

/**
 * Service managing user authentication state, Duende BFF session checks,
 * and secure redirects to Keycloak OpenID Connect flows.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  
  private authStatus = new BehaviorSubject<boolean | null>(null);
  public isAuthenticated$ = this.authStatus.asObservable();

  public get isAuthenticated(): boolean {
    return this.authStatus.value === true;
  }

  // Stores the BFF anti-forgery logout URL containing the session ID (sid) parameter
  private logoutUrl: string | null = null;

  /**
   * Queries the BFF `/bff/user` endpoint to verify active cookie session claims.
   */
  public checkAuth(): Observable<boolean> {
    return this.http.get<any[]>('/bff/user').pipe(
      map(claims => {
        const isAuth = Array.isArray(claims) && claims.length > 0;
        this.authStatus.next(isAuth);

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

  /**
   * Redirects browser to initiate the OIDC challenge via Duende BFF.
   */
  public login(): void {
    window.location.href = '/bff/login';
  }

  /**
   * Cleans up local session and redirects to Keycloak's end_session endpoint.
   */
  public logout(): void {
    window.location.href = this.logoutUrl ?? '/bff/logout';
  }

  /**
   * Redirects to the Keycloak account management console.
   */
  public manageAccount(): void {
    window.location.href = 'http://localhost:8080/realms/normora/account/';
  }
}
