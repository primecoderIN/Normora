import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CreateTenantPayload {
  name: string;
  slug: string;
}

@Injectable({
  providedIn: 'root',
})
export class TenantService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/tenants`;

  createTenant(payload: CreateTenantPayload): Observable<any> {
    return this.http.post<any>(this.apiUrl, payload);
  }
}
