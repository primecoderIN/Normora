import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { Observable, catchError, of, tap } from 'rxjs';

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

  public getMe(): Observable<ApiResponse<CurrentUser>> {
    return this.http.get<ApiResponse<CurrentUser>>(`${this.apiUrl}/me`).pipe(
      tap(response => {
        if (response.success) {
          this.currentUser.set(response.data);
          // Set initial active tenant if not set
          if (!this.activeTenantId() && response.data.memberships.length > 0) {
            // Default to the first non-personal workspace if available, otherwise the personal one
            const defaultWorkspace = response.data.memberships.find(m => !m.isPersonal) || response.data.memberships[0];
            this.activeTenantId.set(defaultWorkspace.tenantId);
          }
        }
      }),
      catchError(error => {
        console.error('Failed to fetch current user profile:', error);
        return of({ success: false, message: 'Failed to load user', data: null as any });
      })
    );
  }
}
