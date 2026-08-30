import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BehaviorSubject, Observable, catchError, of, tap } from 'rxjs';

export interface UserTenantMembership {
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  role: 'admin' | 'employee';
}

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  memberships: UserTenantMembership[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/users`;

  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  public getMe(): Observable<ApiResponse<CurrentUser>> {
    return this.http.get<ApiResponse<CurrentUser>>(`${this.apiUrl}/me`).pipe(
      tap(response => {
        if (response.success) {
          this.currentUserSubject.next(response.data);
        }
      }),
      catchError(error => {
        console.error('Failed to fetch current user profile:', error);
        return of({ success: false, message: 'Failed to load user', data: null as any });
      })
    );
  }

  public getCurrentUserSync(): CurrentUser | null {
    return this.currentUserSubject.value;
  }
}
