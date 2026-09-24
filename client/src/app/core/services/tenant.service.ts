import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

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

  // Submit a new organization creation request to the API, establishing the user as its first Admin
  createTenant(payload: CreateTenantPayload): Observable<any> {
    return this.http.post<any>(this.apiUrl, payload);
  }

  // Retrieve a list of employees for the current tenant
  getEmployees(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/employees`);
  }
}

