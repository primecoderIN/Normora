import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface InvitationDto {
  token: string;
  email: string;
  tenantName: string;
  tenantSlug: string;
  status: string;
  expiresAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class InvitationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api`;

  getInvitation(token: string): Observable<ApiResponse<InvitationDto>> {
    return this.http.get<ApiResponse<InvitationDto>>(`${this.apiUrl}/invitations/${token}`);
  }

  acceptInvitation(token: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/invitations/${token}/accept`, {});
  }

  inviteEmployee(email: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/tenants/invitations`, { email });
  }
}
