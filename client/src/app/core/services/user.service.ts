import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
import { Observable, catchError, of, tap, shareReplay } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';

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
  createdAt: string;
}

/**
 * Service managing user identity, tenant memberships, and active workspace state via Angular Signals.
 */
@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/users`;

  /** Authenticated user profile and memberships signal */
  public currentUser = signal<CurrentUser | null>(null);

  /** Currently selected tenant workspace ID */
  public activeTenantId = signal<string | null>(null);

  /** Active tenant membership computed reactively from activeTenantId or default */
  public activeTenant = computed(() => {
    const user = this.currentUser();
    if (!user) return null;
    const activeId = this.activeTenantId();
    return user.memberships.find(m => m.tenantId === activeId) || user.memberships[0] || null;
  });

  /** Indicates whether the active workspace is the user's personal private space */
  public isPersonalWorkspace = computed(() => this.activeTenant()?.isPersonal ?? false);

  /** Display name of the active workspace */
  public activeTenantName = computed(() => this.activeTenant()?.tenantName || 'your organization');

  private meRequest$?: Observable<ApiResponse<CurrentUser>>;

  /**
   * Retrieves the current user profile, caching the in-flight request and initializing default activeTenantId.
   */
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
