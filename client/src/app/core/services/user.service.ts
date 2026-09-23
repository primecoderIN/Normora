import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { Observable, catchError, of, tap, shareReplay } from 'rxjs';

export interface UserTenantMembership {
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  role: 'admin' | 'employee';
  isPersonal: boolean;
}

export interface PendingInvitation {
  token: string;
  tenantName: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  memberships: UserTenantMembership[];
  pendingInvitations: PendingInvitation[];
}

import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/users`;

  // Application state using Signals
  public currentUser = signal<CurrentUser | null>(null);
  public activeTenantId = signal<string | null>(null);

  private meRequest$?: Observable<ApiResponse<CurrentUser>>;

  public getMe(): Observable<ApiResponse<CurrentUser>> {
    if (this.currentUser()) {
      return of({ success: true, message: '', data: this.currentUser()! });
    }
    
    if (!this.meRequest$) {
      this.meRequest$ = this.http.get<ApiResponse<CurrentUser>>(`${this.apiUrl}/me`).pipe(
        tap(response => {
          if (response.success) {
            this.currentUser.set(response.data);
            if (!this.activeTenantId() && response.data.memberships.length > 0) {
              const defaultWorkspace = response.data.memberships.find(m => !m.isPersonal) || response.data.memberships[0];
              this.activeTenantId.set(defaultWorkspace.tenantId);
            }
          }
        }),
        catchError(error => {
          console.error('Failed to fetch current user profile:', error);
          this.meRequest$ = undefined;
          return of({ success: false, message: 'Failed to load user', data: null as any });
        }),
        shareReplay(1)
      );
    }
    return this.meRequest$;
  }
}
