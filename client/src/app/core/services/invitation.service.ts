import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/api-response.model';

/**
 * Public and administrative invitation details.
 */
export interface InvitationDto {
  token: string;
  email: string;
  tenantName: string;
  tenantSlug: string;
  status: string;
  expiresAt: string;
}

/**
 * Handles employee onboarding invitations, verification of invitation tokens, and member invitations.
 */
@Injectable({
  providedIn: 'root',
})
export class InvitationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api`;

  /**
   * Retrieves public invitation metadata using the secure invitation token.
   */
  getInvitation(token: string): Observable<ApiResponse<InvitationDto>> {
    return this.http.get<ApiResponse<InvitationDto>>(`${this.apiUrl}/invitations/${token}`);
  }

  /**
   * Accepts an invitation, adding the user to the tenant organization.
   */
  acceptInvitation(token: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/invitations/${token}/accept`, {});
  }

  /**
   * Declines an invitation.
   */
  rejectInvitation(token: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/invitations/${token}/reject`, {});
  }

  /**
   * Sends an email invitation to a new employee.
   */
  inviteEmployee(email: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/tenants/invitations`, { email });
  }
}

